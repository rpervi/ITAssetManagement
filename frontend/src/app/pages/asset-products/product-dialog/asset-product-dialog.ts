import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { finalize } from 'rxjs';
import { AssetCategory } from '../../asset-categories/asset-category.models';
import { AssetProduct, SaveAssetProductRequest } from '../asset-product.models';
import { AssetProductService } from '../asset-product.service';

export interface ProductDialogData {
  mode: 'create' | 'edit' | 'view';
  product?: AssetProduct;
  categories: AssetCategory[];
}

export interface ProductDialogResult {
  action: 'created' | 'updated';
  product: AssetProduct;
}

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule,
    ReactiveFormsModule,
  ],
  selector: 'app-asset-product-dialog',
  styleUrl: './asset-product-dialog.css',
  templateUrl: './asset-product-dialog.html',
})
export class AssetProductDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly productService = inject(AssetProductService);
  private readonly dialogRef =
    inject<MatDialogRef<AssetProductDialog, ProductDialogResult | undefined>>(MatDialogRef);
  protected readonly data = inject<ProductDialogData>(MAT_DIALOG_DATA);
  protected readonly isView = this.data.mode === 'view';
  protected readonly isEdit = this.data.mode === 'edit';
  protected submitting = false;
  protected errorMessage = '';

  protected readonly form = this.formBuilder.nonNullable.group({
    categoryId: [this.data.product?.categoryId ?? 0, [Validators.required, Validators.min(1)]],
    productName: [
      this.data.product?.productName ?? '',
      [Validators.required, Validators.maxLength(150)],
    ],
    brand: [this.data.product?.brand ?? '', [Validators.required, Validators.maxLength(100)]],
    model: [this.data.product?.model ?? '', [Validators.maxLength(100)]],
    trackingType: [
      this.data.product?.trackingType ?? 'Individual',
      [Validators.required],
    ],
    minimumStock: [
      this.data.product?.minimumStock ?? 0,
      [Validators.required, Validators.min(0)],
    ],
    isActive: [this.data.product?.isActive ?? true],
  });

  constructor() {
    if (this.isView) {
      this.form.disable();
    }
  }

  protected title(): string {
    if (this.isView) return 'Asset Product Details';
    return this.isEdit ? 'Edit Asset Product' : 'Create Asset Product';
  }

  protected submit(): void {
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const productName = values.productName.trim();
    const brand = values.brand.trim();
    if (!productName || !brand) {
      if (!productName) this.form.controls.productName.setErrors({ required: true });
      if (!brand) this.form.controls.brand.setErrors({ required: true });
      this.form.markAllAsTouched();
      return;
    }

    const request: SaveAssetProductRequest = {
      ...values,
      productName,
      brand,
      model: values.model.trim() || null,
      trackingType: values.trackingType,
    };
    this.submitting = true;
    const operation = this.isEdit
      ? this.productService.update(this.data.product!.productId, request)
      : this.productService.create(request);
    operation.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: (product) =>
        this.dialogRef.close({
          action: this.isEdit ? 'updated' : 'created',
          product,
        }),
      error: (error: HttpErrorResponse) => {
        const validationMessages = Object.values(error.error?.errors ?? {})
          .flat()
          .filter((message): message is string => typeof message === 'string')
          .join(' ');
        this.errorMessage =
          error.error?.detail ??
          validationMessages ??
          error.error?.title ??
          'Could not save this product. Please try again.';
      },
    });
  }
}
