import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { finalize, Observable, Subscription } from 'rxjs';
import { AssetAssignment, AssignmentPage } from '../assignments/assignment.models';
import { AssignmentService } from '../assignments/assignment.service';
import { StockAvailability, StockMovement, PagedStockMovements } from '../stock/stock.models';
import { StockService } from '../stock/stock.service';

type HistoryTab = 'movements' | 'assignments';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
  ],
  selector: 'app-history-page',
  styleUrl: './history-page.css',
  templateUrl: './history-page.html',
})
export class HistoryPage {
  private readonly stockService = inject(StockService);
  private readonly assignmentService = inject(AssignmentService);

  protected readonly activeTab = signal<HistoryTab>('movements');
  protected readonly products = signal<StockAvailability[]>([]);
  protected readonly movements = signal<StockMovement[]>([]);
  protected readonly assignments = signal<AssetAssignment[]>([]);
  protected readonly movementTotalCount = signal(0);
  protected readonly assignmentTotalCount = signal(0);
  protected readonly movementPageIndex = signal(0);
  protected readonly assignmentPageIndex = signal(0);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected productId = 0;
  protected movementType = '';
  protected search = '';
  protected startDate = '';
  protected endDate = '';
  private request?: Subscription;

  constructor() {
    this.stockService.getAvailability().subscribe({
      next: (products) => this.products.set(products),
      error: () => this.loadError.set('Could not load product filters. Please retry.'),
    });
    this.loadData();
  }

  protected selectTab(tab: HistoryTab): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    this.loadData();
  }

  protected loadData(): void {
    this.request?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    const pageIndex = this.activeTab() === 'movements'
      ? this.movementPageIndex()
      : this.assignmentPageIndex();
    const request: Observable<PagedStockMovements | AssignmentPage> = this.activeTab() === 'movements'
      ? this.stockService.getMovements({
        page: pageIndex + 1,
        pageSize: this.pageSize(),
        productId: this.productId || undefined,
        movementType: this.movementType || undefined,
        startDate: this.startDate || undefined,
        endDate: this.endDate || undefined,
      })
      : this.assignmentService.getPage({
        page: pageIndex + 1,
        pageSize: this.pageSize(),
        search: this.search || undefined,
        productId: this.productId || undefined,
        startDate: this.startDate || undefined,
        endDate: this.endDate || undefined,
        sortBy: 'assignedDate',
        sortDirection: 'desc',
      });

    this.request = request
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response: PagedStockMovements | AssignmentPage) => {
          if (this.activeTab() === 'movements') {
            const result = response as PagedStockMovements;
            this.movements.set(result.items);
            this.movementTotalCount.set(result.totalCount);
          } else {
            const result = response as AssignmentPage;
            this.assignments.set(result.items);
            this.assignmentTotalCount.set(result.totalCount);
          }
        },
        error: (error: HttpErrorResponse) =>
          this.loadError.set(
            error.error?.detail ?? 'Could not load asset history. Please retry.',
          ),
      });
  }

  protected updateFilters(): void {
    this.movementPageIndex.set(0);
    this.assignmentPageIndex.set(0);
    this.loadData();
  }

  protected changePage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    if (this.activeTab() === 'movements') {
      this.movementPageIndex.set(event.pageIndex);
    } else {
      this.assignmentPageIndex.set(event.pageIndex);
    }
    this.loadData();
  }

  protected updateSearch(value: string): void {
    this.search = value.trim();
    this.updateFilters();
  }

  protected clearFilters(): void {
    this.search = '';
    this.productId = 0;
    this.movementType = '';
    this.startDate = '';
    this.endDate = '';
    this.updateFilters();
  }

  protected exportCsv(): void {
    const rows: unknown[][] = this.activeTab() === 'movements'
      ? [
        ['Date', 'Movement', 'Product', 'Quantity', 'Asset Code', 'Reference', 'Remarks'],
        ...this.movements().map((movement) => [
          movement.movementDate,
          movement.movementType,
          movement.productName,
          movement.quantity,
          movement.assetCode ?? '',
          movement.referenceId ?? '',
          movement.remarks ?? '',
        ]),
      ]
      : [
        ['Assignment', 'Date', 'Employee', 'Department', 'Items', 'Status', 'Assigned By'],
        ...this.assignments().map((assignment) => [
          assignment.assignmentNumber,
          assignment.assignedDate,
          assignment.employeeName,
          assignment.department,
          assignment.issuedQuantity,
          assignment.status,
          assignment.assignedBy,
        ]),
      ];
    const csv = rows
      .map((row) => row.map((value) => `"${String(value).replaceAll('"', '""')}"`).join(','))
      .join('\r\n');
    const url = URL.createObjectURL(
      new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = `${this.activeTab()}-history.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }
}
