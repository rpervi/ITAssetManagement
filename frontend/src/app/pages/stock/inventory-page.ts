import { DatePipe } from '@angular/common';
import { AfterViewInit, Component, ViewChild, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DecimalPipe } from '@angular/common';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { StockAssetUnit, StockAvailability } from './stock.models';
import { StockService } from './stock.service';

@Component({
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
  ],
  selector: 'app-inventory-page',
  styleUrl: './inventory-page.css',
  templateUrl: './inventory-page.html',
})
export class InventoryPage implements AfterViewInit {
  private readonly stockService = inject(StockService);

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
  protected readonly loadingAssets = signal(false);
  protected readonly assetError = signal('');

  constructor() {
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
        next: (availability) => (this.dataSource.data = availability),
        error: () =>
          this.loadError.set('Could not load inventory availability. Please retry.'),
      });
  }

  protected applySearch(value: string): void {
    this.dataSource.filter = value.trim().toLowerCase();
    this.dataSource.paginator?.firstPage();
  }

  protected viewAssets(product: StockAvailability): void {
    this.selectedProduct.set(product);
    this.assets.set([]);
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
}
