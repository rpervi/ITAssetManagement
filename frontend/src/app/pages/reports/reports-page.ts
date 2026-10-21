import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { forkJoin, Observable, of, switchMap } from 'rxjs';
import { AssignmentPage, AssetAssignment } from '../assignments/assignment.models';
import { AssignmentService } from '../assignments/assignment.service';
import { StockAvailability } from '../stock/stock.models';
import { StockService } from '../stock/stock.service';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  selector: 'app-reports-page',
  styleUrl: './reports-page.css',
  templateUrl: './reports-page.html',
})
export class ReportsPage {
  private readonly stockService = inject(StockService);
  private readonly assignmentService = inject(AssignmentService);

  protected readonly inventory = signal<StockAvailability[]>([]);
  protected readonly assignmentRows = signal<AssetAssignment[]>([]);
  protected readonly assignmentCount = signal(0);
  protected readonly assignmentLoading = signal(false);
  protected readonly inventoryLoading = signal(false);
  protected readonly inventoryError = signal('');
  protected readonly assignmentError = signal('');
  protected readonly startDate = signal('');
  protected readonly endDate = signal('');
  protected readonly availableTotal = computed(() =>
    this.inventory().reduce((sum, product) => sum + product.availableQuantity, 0),
  );
  protected readonly assignedTotal = computed(() =>
    this.inventory().reduce((sum, product) => sum + product.assignedQuantity, 0),
  );
  protected readonly inventoryValue = computed(() =>
    this.inventory().reduce(
      (sum, product) => sum + product.availableQuantity
        + product.assignedQuantity
        + product.unavailableQuantity,
      0,
    ),
  );
  protected readonly issuedTotal = computed(() =>
    this.assignmentRows().reduce((sum, assignment) => sum + assignment.issuedQuantity, 0),
  );
  protected readonly returnedTotal = computed(() =>
    this.assignmentRows().reduce(
      (sum, assignment) => sum + assignment.items.reduce((total, item) => total + item.returnedQuantity, 0),
      0,
    ),
  );
  protected readonly outstandingTotal = computed(() =>
    this.assignmentRows().reduce(
      (sum, assignment) => sum + assignment.items.reduce((total, item) => total + item.outstandingQuantity, 0),
      0,
    ),
  );

  constructor() {
    this.loadInventory();
    this.loadAssignments();
  }

  protected loadInventory(): void {
    this.inventoryLoading.set(true);
    this.inventoryError.set('');
    this.stockService.getAvailability().subscribe({
      next: (inventory) => {
        this.inventory.set(inventory);
        this.inventoryLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.inventoryError.set(
          error.error?.detail ?? 'Could not load inventory report. Please retry.',
        );
        this.inventoryLoading.set(false);
      },
    });
  }

  protected loadAssignments(): void {
    this.assignmentLoading.set(true);
    this.assignmentError.set('');
    this.getAllAssignmentPages().subscribe({
      next: ({ assignments, totalCount }) => {
        this.assignmentRows.set(assignments);
        this.assignmentCount.set(totalCount);
        this.assignmentLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.assignmentError.set(
          error.error?.detail ?? 'Could not load assignment report. Please retry.',
        );
        this.assignmentLoading.set(false);
      },
    });
  }

  protected updateDateRange(startDate: string, endDate: string): void {
    this.startDate.set(startDate);
    this.endDate.set(endDate);
    this.loadAssignments();
  }

  protected exportInventoryCsv(): void {
    const headers = [
      'Product',
      'Category',
      'Tracking Type',
      'Available',
      'Assigned',
      'Unavailable',
      'Damaged',
      'Minimum Stock',
      'Stock Status',
    ];
    const rows = this.inventory().map((product) => [
      product.productName,
      product.categoryName,
      product.trackingType,
      product.availableQuantity,
      product.assignedQuantity,
      product.unavailableQuantity,
      product.damagedQuantity,
      product.minimumStock,
      product.minimumStock > 0 && product.availableQuantity <= product.minimumStock
        ? 'Low stock'
        : 'In stock',
    ]);
    this.downloadCsv('inventory-report.csv', [headers, ...rows]);
  }

  protected exportAssignmentsCsv(): void {
    const headers = [
      'Assignment',
      'Assigned Date',
      'Employee',
      'Employee Code',
      'Department',
      'Product',
      'Tracking Type',
      'Quantity',
      'Returned',
      'Outstanding',
      'Status',
      'Assigned By',
    ];
    const rows = this.assignmentRows().flatMap((assignment) =>
      assignment.items.map((item) => [
        assignment.assignmentNumber,
        assignment.assignedDate,
        assignment.employeeName,
        assignment.employeeCode,
        assignment.department,
        item.productName,
        item.trackingType,
        item.quantity,
        item.returnedQuantity,
        item.outstandingQuantity,
        assignment.status,
        assignment.assignedBy,
      ]),
    );
    this.downloadCsv('assignment-report.csv', [headers, ...rows]);
  }

  private getAllAssignmentPages(): Observable<{
    assignments: AssetAssignment[];
    totalCount: number;
  }> {
    return this.assignmentService.getPage({
      page: 1,
      pageSize: 100,
      startDate: this.startDate() || undefined,
      endDate: this.endDate() || undefined,
      sortBy: 'assignedDate',
      sortDirection: 'desc',
    }).pipe(
      switchMap((firstPage) => {
        const pageCount = Math.ceil(firstPage.totalCount / firstPage.pageSize);
        if (pageCount <= 1) {
          return of({
            assignments: firstPage.items,
            totalCount: firstPage.totalCount,
          });
        }

        const remainingPages = Array.from(
          { length: pageCount - 1 },
          (_, index) => this.assignmentService.getPage({
            page: index + 2,
            pageSize: firstPage.pageSize,
            startDate: this.startDate() || undefined,
            endDate: this.endDate() || undefined,
            sortBy: 'assignedDate',
            sortDirection: 'desc',
          }),
        );
        return forkJoin(remainingPages).pipe(
          switchMap((pages) =>
            of({
              assignments: [
                ...firstPage.items,
                ...pages.flatMap((page: AssignmentPage) => page.items),
              ],
              totalCount: firstPage.totalCount,
            }),
          ),
        );
      }),
    );
  }

  private downloadCsv(filename: string, rows: unknown[][]): void {
    const csv = rows
      .map((row) => row.map((value) => this.csvCell(value)).join(','))
      .join('\r\n');
    const url = URL.createObjectURL(
      new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  }

  private csvCell(value: unknown): string {
    const text = String(value ?? '');
    const safeText = /^[\s]*[=+\-@\t\r]/.test(text) ? `'${text}` : text;
    return `"${safeText.replaceAll('"', '""')}"`;
  }
}
