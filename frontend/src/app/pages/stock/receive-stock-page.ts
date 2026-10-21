import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  IndividualStockAssetRequest,
  ReceiveBulkStockRequest,
  ReceiveIndividualStockRequest,
  StockProduct,
} from './stock.models';
import { StockService } from './stock.service';

type StockMode = 'Individual' | 'Bulk';
type StockCondition = 'New' | 'Good' | 'Damaged';

@Component({
  imports: [
    MatButtonModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  selector: 'app-receive-stock-page',
  styleUrl: './receive-stock-page.css',
  templateUrl: './receive-stock-page.html',
})
export class ReceiveStockPage {
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly stockService = inject(StockService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly products = signal<StockProduct[]>([]);
  protected readonly selectedProduct = signal<StockProduct | null>(null);
  protected readonly mode = signal<StockMode>('Individual');
  protected readonly loadingProducts = signal(false);
  protected readonly productsError = signal('');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');
  private idempotencyKey = crypto.randomUUID();

  protected readonly form = this.formBuilder.group({
    productId: [0, [Validators.required, Validators.min(1)]],
    supplier: ['', [Validators.maxLength(200)]],
    invoiceNumber: ['', [Validators.maxLength(100)]],
    remarks: ['', [Validators.maxLength(1000)]],
    assets: this.formBuilder.array([this.createAssetRow()]),
    quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    purchaseDate: ['', [Validators.required]],
    warrantyExpiryDate: [''],
    condition: ['Good', [Validators.required]],
  });

  constructor() {
    this.configureMode(this.mode());
    this.loadProducts();
  }

  protected get assetRows(): FormArray {
    return this.form.controls.assets;
  }

  protected productsForMode(): StockProduct[] {
    return this.products().filter((product) => product.trackingType === this.mode());
  }

  protected setMode(mode: StockMode): void {
    this.mode.set(mode);
    this.selectedProduct.set(null);
    this.form.controls.productId.setValue(0);
    this.form.controls.productId.markAsPristine();
    this.configureMode(mode);
  }

  protected selectProduct(productId: number): void {
    const product = this.products().find((item) => item.productId === productId) ?? null;
    this.selectedProduct.set(product);
    if (product) {
      this.mode.set(product.trackingType);
      this.configureMode(product.trackingType);
    }
  }

  protected loadProducts(): void {
    this.loadingProducts.set(true);
    this.productsError.set('');
    this.stockService
      .getProducts()
      .pipe(finalize(() => this.loadingProducts.set(false)))
      .subscribe({
        next: (products) => this.products.set(products),
        error: () => this.productsError.set('Could not load active products. Please retry.'),
      });
  }

  protected addAssetRow(): void {
    this.assetRows.push(this.createAssetRow());
  }

  protected removeAssetRow(index: number): void {
    if (this.assetRows.length > 1) {
      this.assetRows.removeAt(index);
    }
  }

  protected submit(): void {
    this.errorMessage.set('');
    if (this.form.invalid || !this.selectedProduct()) {
      this.form.markAllAsTouched();
      if (!this.selectedProduct()) {
        this.form.controls.productId.markAsTouched();
      }
      return;
    }

    if (this.mode() === 'Individual') {
      const assets = this.assetRows.getRawValue();
      if (!this.validateDates()) {
        return;
      }

      const request: ReceiveIndividualStockRequest = {
        idempotencyKey: this.idempotencyKey,
        productId: this.selectedProduct()!.productId,
        assets: assets.map((asset) => ({
          serialNumber: asset.serialNumber.trim() || null,
          purchaseDate: asset.purchaseDate,
          warrantyExpiryDate: asset.warrantyExpiryDate || null,
          purchaseCost: Number(asset.purchaseCost),
          condition: this.toCondition(asset.condition),
        })),
        supplier: this.form.controls.supplier.value.trim() || null,
        invoiceNumber: this.form.controls.invoiceNumber.value.trim() || null,
        remarks: this.form.controls.remarks.value.trim() || null,
      };
      this.submitting.set(true);
      this.stockService
        .receiveIndividual(request)
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe({
          next: (receipt) => this.receiveSucceeded(receipt.receiptNumber),
          error: (error: HttpErrorResponse) => this.showApiError(error),
        });
      return;
    }

    const bulk = this.form.getRawValue();
    const purchaseDate = bulk.purchaseDate;
    if (bulk.warrantyExpiryDate && bulk.warrantyExpiryDate < purchaseDate) {
      this.form.controls.warrantyExpiryDate.setErrors({ warrantyBeforePurchase: true });
      this.form.controls.warrantyExpiryDate.markAsTouched();
      return;
    }

    const request: ReceiveBulkStockRequest = {
      idempotencyKey: this.idempotencyKey,
      productId: this.selectedProduct()!.productId,
      quantity: Number(bulk.quantity),
      unitPrice: Number(bulk.unitPrice),
      purchaseDate,
      warrantyExpiryDate: bulk.warrantyExpiryDate || null,
      condition: this.toCondition(bulk.condition),
      supplier: bulk.supplier.trim() || null,
      invoiceNumber: bulk.invoiceNumber.trim() || null,
      remarks: bulk.remarks.trim() || null,
    };
    this.submitting.set(true);
    this.stockService
      .receiveBulk(request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (receipt) => this.receiveSucceeded(receipt.receiptNumber),
        error: (error: HttpErrorResponse) => this.showApiError(error),
      });
  }

  private createAssetRow() {
    return this.formBuilder.group({
      serialNumber: ['', [Validators.maxLength(100)]],
      purchaseDate: ['', [Validators.required]],
      warrantyExpiryDate: [''],
      purchaseCost: [0, [Validators.required, Validators.min(0)]],
      condition: ['Good', [Validators.required]],
    });
  }

  private configureMode(mode: StockMode): void {
    const bulkControls = [
      this.form.controls.quantity,
      this.form.controls.unitPrice,
      this.form.controls.purchaseDate,
      this.form.controls.warrantyExpiryDate,
      this.form.controls.condition,
    ];

    if (mode === 'Individual') {
      this.assetRows.enable({ emitEvent: false });
      bulkControls.forEach((control) => control.disable({ emitEvent: false }));
      return;
    }

    this.assetRows.disable({ emitEvent: false });
    bulkControls.forEach((control) => control.enable({ emitEvent: false }));
  }

  private validateDates(): boolean {
    let valid = true;
    this.assetRows.controls.forEach((control) => {
      const values = control.getRawValue() as {
        purchaseDate: string;
        warrantyExpiryDate: string;
      };
      const warranty = control.get('warrantyExpiryDate');
      warranty?.setErrors(null);
      if (values.warrantyExpiryDate && values.warrantyExpiryDate < values.purchaseDate) {
        warranty?.setErrors({ warrantyBeforePurchase: true });
        warranty?.markAsTouched();
        valid = false;
      }
    });
    return valid;
  }

  private toCondition(value: string): StockCondition {
    if (value === 'New' || value === 'Damaged') return value;
    return 'Good';
  }

  private receiveSucceeded(receiptNumber: string): void {
    this.snackBar.open(`Stock received. Receipt ${receiptNumber}.`, 'Dismiss', {
      duration: 5000,
    });
    this.form.reset({
      productId: 0,
      supplier: '',
      invoiceNumber: '',
      remarks: '',
      quantity: 1,
      unitPrice: 0,
      purchaseDate: '',
      warrantyExpiryDate: '',
      condition: 'Good',
    });
    this.assetRows.clear();
    this.assetRows.push(this.createAssetRow());
    this.selectedProduct.set(null);
    this.idempotencyKey = crypto.randomUUID();
  }

  private showApiError(error: HttpErrorResponse): void {
    const errors = Object.values(error.error?.errors ?? {})
      .flat()
      .filter((message): message is string => typeof message === 'string')
      .join(' ');
    const message =
      error.error?.detail ??
      (errors || error.error?.title || 'Could not receive stock. Your entered values are preserved.');
    this.errorMessage.set(message);
    this.snackBar.open(message, 'Dismiss', { duration: 6000 });
  }
}
