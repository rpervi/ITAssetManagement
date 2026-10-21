import { DatePipe } from '@angular/common';
import { AfterViewInit, Component, ViewChild, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DecimalPipe } from '@angular/common';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AssetBarcode } from '../../shared/asset-barcode/asset-barcode';
import { BarcodeLabelPrinter } from '../../shared/barcode-label-printer/barcode-label-printer';
import { downloadCsv } from '../../shared/csv-export';
import { PrintableAsset, StockAssetUnit, StockAvailability } from './stock.models';
import { StockService } from './stock.service';

@Component({
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    RouterLink,
    AssetBarcode,
    BarcodeLabelPrinter,
  ],
  selector: 'app-inventory-page',
  styleUrl: './inventory-page.css',
  templateUrl: './inventory-page.html',
})
export class InventoryPage implements AfterViewInit {
  private readonly stockService = inject(StockService);
  private readonly route = inject(ActivatedRoute);

  @ViewChild(MatSort) private sort!: MatSort;
  @ViewChild(MatPaginator) private paginator!: MatPaginator;

  protected readonly displayedColumns = [
    'productName',
    'categoryName',
    'trackingType',
    'availableQuantity',
    'assignedQuantity',
    'unavailableQuantity',
    'damagedQuantity',
    'minimumStock',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<StockAvailability>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly selectedProduct = signal<StockAvailability | null>(null);
  protected readonly assets = signal<StockAssetUnit[]>([]);
  protected readonly selectedAssetIds = signal<number[]>([]);
  protected readonly focusAssetUnitId = signal<number | null>(null);
  private pendingProductId?: number;
  protected readonly loadingAssets = signal(false);
  protected readonly assetError = signal('');

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const productId = Number(params.get('productId'));
      const assetUnitId = Number(params.get('assetUnitId'));
      this.focusAssetUnitId.set(Number.isInteger(assetUnitId) && assetUnitId > 0 ? assetUnitId : null);
      this.pendingProductId = Number.isInteger(productId) && productId > 0 ? productId : undefined;
      const product = this.dataSource.data.find((item) => item.productId === this.pendingProductId);
      if (product) this.viewAssets(product);
    });
    this.dataSource.filterPredicate = (product, filter) =>
      `${product.productName} ${product.categoryName} ${product.trackingType}`
        .toLowerCase()
        .includes(filter);
    this.loadAvailability();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (product, property) => {
      switch (property) {
        case 'productName':
        case 'categoryName':
        case 'trackingType':
          return product[property].toLowerCase();
        case 'availableQuantity':
          return product.availableQuantity;
        case 'assignedQuantity':
          return product.assignedQuantity;
        case 'unavailableQuantity':
          return product.unavailableQuantity;
        case 'damagedQuantity':
          return product.damagedQuantity;
        case 'minimumStock':
          return product.minimumStock;
        default:
          return '';
      }
    };
  }

  protected loadAvailability(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.stockService
      .getAvailability()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (availability) => {
          this.dataSource.data = availability;
          const product = availability.find((item) => item.productId === this.pendingProductId);
          if (product) this.viewAssets(product);
        },
        error: () =>
          this.loadError.set('Could not load inventory availability. Please retry.'),
      });
  }

  protected applySearch(value: string): void {
    this.dataSource.filter = value.trim().toLowerCase();
    this.dataSource.paginator?.firstPage();
  }

  protected exportCsv(): void {
    const rows = this.dataSource.filteredData.map((product) => [
      product.productId,
      product.productName,
      product.categoryName,
      product.trackingType,
      product.availableQuantity,
      product.assignedQuantity,
      product.unavailableQuantity,
      product.damagedQuantity,
      product.minimumStock,
    ]);
    downloadCsv(
      'inventory.csv',
      [
        'Product ID',
        'Product',
        'Category',
        'Tracking Type',
        'Available / Usable',
        'Assigned',
        'Unavailable',
        'Damaged',
        'Minimum Stock',
      ],
      rows,
    );
  }

  protected viewAssets(product: StockAvailability): void {
    this.selectedProduct.set(product);
    this.assets.set([]);
    this.selectedAssetIds.set([]);
    this.assetError.set('');
    if (product.trackingType !== 'Individual') return;
    this.loadingAssets.set(true);
    this.stockService
      .getUnits(product.productId)
      .pipe(finalize(() => this.loadingAssets.set(false)))
      .subscribe({
        next: (units) => this.assets.set(units),
        error: (error: HttpErrorResponse) =>
          this.assetError.set(
            error.error?.detail ?? 'Could not load asset details. Please retry.',
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
    this.selectedAssetIds.set(
      selected ? this.assets().map((asset) => asset.assetUnitId) : [],
    );
  }

  protected isAssetSelected(assetUnitId: number): boolean {
    return this.selectedAssetIds().includes(assetUnitId);
  }

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
}
