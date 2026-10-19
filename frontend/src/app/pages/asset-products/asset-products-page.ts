import { AfterViewInit, Component, ViewChild, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize, forkJoin } from 'rxjs';
import { AssetCategoryService } from '../asset-categories/asset-category.service';
import { AssetCategory } from '../asset-categories/asset-category.models';
import { AssetProduct } from './asset-product.models';
import {
  AssetProductDialog,
  ProductDialogData,
  ProductDialogResult,
} from './product-dialog/asset-product-dialog';
import { AssetProductService } from './asset-product.service';

interface ProductFilters {
  search: string;
  categoryId: string;
  trackingType: string;
  status: string;
}

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
  ],
  selector: 'app-asset-products-page',
  styleUrl: './asset-products-page.css',
  templateUrl: './asset-products-page.html',
})
export class AssetProductsPage implements AfterViewInit {
  private readonly productService = inject(AssetProductService);
  private readonly categoryService = inject(AssetCategoryService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild(MatSort) private sort!: MatSort;
  @ViewChild(MatPaginator) private paginator!: MatPaginator;

  protected readonly displayedColumns = [
    'productName',
    'categoryName',
    'brand',
    'model',
    'trackingType',
    'minimumStock',
    'status',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<AssetProduct>([]);
  protected readonly categories = signal<AssetCategory[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly statusUpdating = signal<number | null>(null);
  private filters: ProductFilters = {
    search: '',
    categoryId: '',
    trackingType: '',
    status: '',
  };

  constructor() {
    this.dataSource.filterPredicate = (product, filterText) => {
      const filters = JSON.parse(filterText) as ProductFilters;
      const searchable = [
        product.productName,
        product.categoryName,
        product.brand,
        product.model ?? '',
        product.trackingType,
      ]
        .join(' ')
        .toLowerCase();
      return (
        searchable.includes(filters.search) &&
        (!filters.categoryId || product.categoryId.toString() === filters.categoryId) &&
        (!filters.trackingType || product.trackingType === filters.trackingType) &&
        (!filters.status ||
          (filters.status === 'active' ? product.isActive : !product.isActive))
      );
    };
    this.loadData();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (product, property) => {
      switch (property) {
        case 'productName':
        case 'categoryName':
        case 'brand':
        case 'model':
        case 'trackingType':
          return (product[property] ?? '').toLowerCase();
        case 'minimumStock':
          return product.minimumStock;
        case 'status':
          return product.isActive ? 1 : 0;
        default:
          return '';
      }
    };
  }

  protected loadData(): void {
    this.loading.set(true);
    this.loadError.set('');
    forkJoin({
      products: this.productService.getAll(),
      categories: this.categoryService.getAll(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ products, categories }) => {
          this.dataSource.data = products;
          this.categories.set(categories);
          this.applyFilters();
        },
        error: () => this.loadError.set('Could not load products. Please refresh and try again.'),
      });
  }

  protected updateSearch(value: string): void {
    this.filters.search = value.trim().toLowerCase();
    this.applyFilters();
  }

  protected updateCategory(categoryId: string): void {
    this.filters.categoryId = categoryId;
    this.applyFilters();
  }

  protected updateTrackingType(trackingType: string): void {
    this.filters.trackingType = trackingType;
    this.applyFilters();
  }

  protected updateStatus(status: string): void {
    this.filters.status = status;
    this.applyFilters();
  }

  protected clearFilters(searchInput: HTMLInputElement): void {
    searchInput.value = '';
    this.filters = { search: '', categoryId: '', trackingType: '', status: '' };
    this.applyFilters();
  }

  protected openDialog(mode: ProductDialogData['mode'], product?: AssetProduct): void {
    const dialogRef = this.dialog.open<
      AssetProductDialog,
      ProductDialogData,
      ProductDialogResult | undefined
    >(AssetProductDialog, {
      width: '620px',
      maxWidth: 'calc(100vw - 32px)',
      autoFocus: 'first-tabbable',
      data: { mode, product, categories: this.categories() },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;
      this.snackBar.open(
        result.action === 'created' ? 'Product created.' : 'Product updated.',
        'Dismiss',
        { duration: 3500 },
      );
      this.loadData();
    });
  }

  protected toggleStatus(product: AssetProduct): void {
    const isActive = !product.isActive;
    this.statusUpdating.set(product.productId);
    this.productService
      .setStatus(product.productId, isActive)
      .pipe(finalize(() => this.statusUpdating.set(null)))
      .subscribe({
        next: (updated) => {
          this.dataSource.data = this.dataSource.data.map((item) =>
            item.productId === updated.productId ? updated : item,
          );
          this.snackBar.open(
            `Product ${updated.isActive ? 'activated' : 'deactivated'}.`,
            'Dismiss',
            { duration: 3500 },
          );
        },
        error: (error: HttpErrorResponse) => {
          this.snackBar.open(
            error.error?.detail ?? 'Could not update product status. Please try again.',
            'Dismiss',
            { duration: 5000 },
          );
        },
      });
  }

  protected exportExcel(): void {
    const columns = [
      'Product Name',
      'Category',
      'Brand',
      'Model',
      'Tracking Type',
      'Minimum Stock',
      'Status',
    ];
    const escapeXml = (value: string): string =>
      value.replace(/[<>&"']/g, (character) => {
        const entities: Record<string, string> = {
          '<': '&lt;',
          '>': '&gt;',
          '&': '&amp;',
          '"': '&quot;',
          "'": '&apos;',
        };
        return entities[character];
      });
    const cell = (value: string | number): string => {
      const type = typeof value === 'number' ? 'Number' : 'String';
      return `<Cell><Data ss:Type="${type}">${escapeXml(String(value))}</Data></Cell>`;
    };
    const dataRows = this.dataSource.filteredData.map((product) => [
      product.productName,
      product.categoryName,
      product.brand,
      product.model ?? '',
      product.trackingType,
      product.minimumStock,
      product.isActive ? 'Active' : 'Inactive',
    ]);
    const workbook = [
      '<?xml version="1.0"?>',
      '<?mso-application progid="Excel.Sheet"?>',
      '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">',
      '<Worksheet ss:Name="Asset Products"><Table>',
      `<Row>${columns.map(cell).join('')}</Row>`,
      ...dataRows.map((row) => `<Row>${row.map(cell).join('')}</Row>`),
      '</Table></Worksheet></Workbook>',
    ].join('');
    const url = URL.createObjectURL(
      new Blob([workbook], { type: 'application/vnd.ms-excel;charset=utf-8' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = 'asset-products.xls';
    link.click();
    URL.revokeObjectURL(url);
  }

  private applyFilters(): void {
    this.dataSource.filter = JSON.stringify(this.filters);
    this.dataSource.paginator?.firstPage();
  }
}
