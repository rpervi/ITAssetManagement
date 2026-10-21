import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { AssetBarcode } from '../../shared/asset-barcode/asset-barcode';
import { BarcodeLabelPrinter } from '../../shared/barcode-label-printer/barcode-label-printer';
import {
  PrintableAsset,
  StockAssetUnit,
  StockAvailability,
} from '../stock/stock.models';
import { StockService } from '../stock/stock.service';

@Component({
  imports: [
    AssetBarcode,
    BarcodeLabelPrinter,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  selector: 'app-generate-barcodes-page',
  styleUrl: './generate-barcodes-page.css',
  templateUrl: './generate-barcodes-page.html',
})
export class GenerateBarcodesPage {
  private readonly stockService = inject(StockService);
  private readonly authService = inject(AuthService);

  protected readonly products = signal<StockAvailability[]>([]);
  protected readonly selectedProduct = signal<StockAvailability | null>(null);
  protected readonly conversionProductId = signal<number | null>(null);
  protected readonly assets = signal<StockAssetUnit[]>([]);
  protected readonly selectedAssetIds = signal<number[]>([]);
  protected readonly loadingProducts = signal(false);
  protected readonly loadingAssets = signal(false);
  protected readonly convertingProductId = signal<number | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly assetError = signal('');
  protected readonly successMessage = signal('');
  protected readonly canConvertBulk = computed(() => {
    const roles = this.authService.currentUser()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('ITStaff');
  });

  protected readonly printableAssets = computed(() => {
    const selectedIds = new Set(this.selectedAssetIds());
    return this.assets()
      .filter((asset) => selectedIds.has(asset.assetUnitId))
      .map((asset) => ({
        assetUnitId: asset.assetUnitId,
        assetCode: asset.assetCode,
        assetName: this.selectedProduct()?.productName ?? '',
      }));
  });

  constructor() {
    this.loadProducts();
  }

  protected loadProducts(): void {
    this.loadingProducts.set(true);
    this.errorMessage.set('');
    this.stockService
      .getAvailability()
      .pipe(finalize(() => this.loadingProducts.set(false)))
      .subscribe({
        next: (products) => this.products.set(
          products.filter((product) =>
            product.trackingType === 'Individual'
            || product.availableQuantity > 0
            || product.assignedQuantity > 0
            || product.unavailableQuantity > 0),
        ),
        error: () => this.errorMessage.set('Could not load inventory products. Please retry.'),
      });
  }

  protected requestConversion(product: StockAvailability): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.conversionProductId.set(product.productId);
  }

  protected cancelConversion(): void {
    this.conversionProductId.set(null);
  }

  protected convertBulkProduct(product: StockAvailability): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.convertingProductId.set(product.productId);
    this.stockService
      .convertBulkStockToIndividual(product.productId)
      .pipe(finalize(() => this.convertingProductId.set(null)))
      .subscribe({
        next: (result) => {
          this.conversionProductId.set(null);
          this.successMessage.set(
            `Created ${result.assetsCreated} individually tracked asset(s) with unique barcodes.`,
          );
          const individualProduct = { ...product, trackingType: 'Individual' as const };
          this.selectedProduct.set(individualProduct);
          this.loadProducts();
          this.loadAssets(individualProduct, true);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            error.error?.detail ?? 'Could not convert bulk stock. Please refresh and try again.',
          );
        },
      });
  }

  protected loadAssets(product: StockAvailability, selectAll = false): void {
    this.selectedProduct.set(product);
    this.assets.set([]);
    this.selectedAssetIds.set([]);
    this.assetError.set('');
    this.loadingAssets.set(true);
    this.stockService
      .getUnits(product.productId)
      .pipe(finalize(() => this.loadingAssets.set(false)))
      .subscribe({
        next: (assets) => {
          this.assets.set(assets);
          if (selectAll) {
            this.selectedAssetIds.set(assets.map((asset) => asset.assetUnitId));
          }
        },
        error: (error: HttpErrorResponse) =>
          this.assetError.set(
            error.error?.detail ?? 'Could not load this product’s assets. Please retry.',
          ),
      });
  }

  protected toggleAsset(assetUnitId: number, selected: boolean): void {
    const selectedIds = new Set(this.selectedAssetIds());
    if (selected) selectedIds.add(assetUnitId);
    else selectedIds.delete(assetUnitId);
    this.selectedAssetIds.set([...selectedIds]);
  }

  protected toggleAllAssets(selected: boolean): void {
    this.selectedAssetIds.set(selected ? this.assets().map((asset) => asset.assetUnitId) : []);
  }

  protected isAssetSelected(assetUnitId: number): boolean {
    return this.selectedAssetIds().includes(assetUnitId);
  }
}
