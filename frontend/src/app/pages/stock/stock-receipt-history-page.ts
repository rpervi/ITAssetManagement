import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { StockAvailability, StockReceipt, StockReceiptSummary } from './stock.models';
import { StockReceiptQuery, StockService } from './stock.service';

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
    MatSelectModule,
    MatSortModule,
    MatTableModule,
  ],
  selector: 'app-stock-receipt-history-page',
  styleUrl: './stock-receipt-history-page.css',
  templateUrl: './stock-receipt-history-page.html',
})
export class StockReceiptHistoryPage {
  private readonly stockService = inject(StockService);
  protected readonly displayedColumns = [
    'receiptNumber',
    'receivedDate',
    'productName',
    'trackingType',
    'quantity',
    'supplier',
    'invoiceNumber',
    'totalAmount',
    'receivedBy',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<StockReceiptSummary>([]);
  protected readonly products = signal<StockAvailability[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadingDetails = signal(false);
  protected readonly loadError = signal('');
  protected readonly detailsError = signal('');
  protected readonly selectedReceipt = signal<StockReceipt | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);
  protected search = '';
  protected productId = 0;
  protected startDate = '';
  protected endDate = '';
  protected sortBy = 'receivedDate';
  protected sortDirection: SortDirection = 'desc';

  constructor() {
    this.loadProducts();
    this.loadReceipts();
  }

  protected loadReceipts(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.stockService
      .getReceipts(this.query())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.dataSource.data = response.items;
          this.totalCount.set(response.totalCount);
        },
        error: (error: HttpErrorResponse) => {
          this.loadError.set(
            error.error?.detail ?? 'Could not load stock receipt history. Please retry.',
          );
        },
      });
  }

  protected applySearch(value: string): void {
    this.search = value.trim();
    this.pageIndex.set(0);
    this.loadReceipts();
  }

  protected setProduct(productId: number): void {
    this.productId = productId;
    this.pageIndex.set(0);
    this.loadReceipts();
  }

  protected setDateRange(startDate: string, endDate: string): void {
    this.startDate = startDate;
    this.endDate = endDate;
    this.pageIndex.set(0);
    this.loadReceipts();
  }

  protected changePage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadReceipts();
  }

  protected changeSort(sort: Sort): void {
    if (!sort.direction) return;
    this.sortBy = sort.active;
    this.sortDirection = sort.direction;
    this.pageIndex.set(0);
    this.loadReceipts();
  }

  protected clearFilters(searchInput: HTMLInputElement): void {
    searchInput.value = '';
    this.search = '';
    this.productId = 0;
    this.startDate = '';
    this.endDate = '';
    this.pageIndex.set(0);
    this.loadReceipts();
  }

  protected viewReceipt(receiptId: number): void {
    this.loadingDetails.set(true);
    this.detailsError.set('');
    this.selectedReceipt.set(null);
    this.stockService
      .getReceipt(receiptId)
      .pipe(finalize(() => this.loadingDetails.set(false)))
      .subscribe({
        next: (receipt) => this.selectedReceipt.set(receipt),
        error: (error: HttpErrorResponse) =>
          this.detailsError.set(
            error.error?.detail ?? 'Could not load receipt details. Please retry.',
          ),
      });
  }

  protected exportExcel(): void {
    const columns = [
      'Receipt Number',
      'Received Date',
      'Product',
      'Tracking Type',
      'Quantity',
      'Supplier',
      'Invoice Number',
      'Total Amount',
      'Received By',
    ];
    const rows = this.dataSource.data.map((receipt) => [
      receipt.receiptNumber,
      receipt.receivedDate,
      receipt.productName,
      receipt.trackingType,
      receipt.quantity,
      receipt.supplier ?? '',
      receipt.invoiceNumber ?? '',
      receipt.totalAmount,
      receipt.receivedBy ?? '',
    ]);
    const csv = [columns, ...rows]
      .map((row) => row.map((value) => `"${String(value).replaceAll('"', '""')}"`).join(','))
      .join('\r\n');
    const url = URL.createObjectURL(
      new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = 'stock-receipt-history.csv';
    link.click();
    URL.revokeObjectURL(url);
  }

  private loadProducts(): void {
    this.stockService.getAvailability().subscribe({
      next: (products) => this.products.set(products),
      error: () =>
        this.loadError.set('Could not load products for the history filter. Please retry.'),
    });
  }

  private query(): StockReceiptQuery {
    return {
      search: this.search || undefined,
      productId: this.productId || undefined,
      startDate: this.startDate || undefined,
      endDate: this.endDate || undefined,
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
    };
  }
}
