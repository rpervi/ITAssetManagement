import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort, SortDirection } from '@angular/material/sort';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { finalize, Subscription } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { AssignmentDialog, AssignmentDialogData } from './assignment-dialog/assignment-dialog';
import { AssignmentProduct, AssetAssignment } from './assignment.models';
import { AssignmentService } from './assignment.service';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
  ],
  selector: 'app-assignments-page',
  styleUrl: './assignments-page.css',
  templateUrl: './assignments-page.html',
})
export class AssignmentsPage {
  private readonly assignmentService = inject(AssignmentService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = [
    'assignmentNumber',
    'employeeName',
    'assignedDate',
    'issuedQuantity',
    'status',
    'assignedBy',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<AssetAssignment>([]);
  protected readonly products = signal<AssignmentProduct[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly isAssignmentStaff = computed(() => {
    const roles = this.authService.currentUser()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('ITStaff');
  });
  private search = '';
  private productId?: number;
  private sortBy = 'assignedDate';
  private sortDirection: SortDirection = 'desc';
  private listRequest?: Subscription;

  constructor() {
    this.dataSource.sortingDataAccessor = (assignment, property) => {
      switch (property) {
        case 'assignmentNumber':
          return assignment.assignmentId;
        case 'employeeName':
          return assignment.employeeName.toLocaleLowerCase();
        case 'assignedDate':
          return new Date(assignment.assignedDate).getTime();
        case 'issuedQuantity':
          return assignment.issuedQuantity;
        case 'status':
          return assignment.status;
        case 'assignedBy':
          return assignment.assignedBy.toLocaleLowerCase();
        default:
          return '';
      }
    };
    this.assignmentService.getActiveProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () =>
        this.snackBar.open(
          'Could not load product filters. You can still search assignment history.',
          'Dismiss',
          { duration: 5000 },
        ),
    });
    this.loadData();
  }

  protected loadData(): void {
    this.listRequest?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    this.listRequest = this.assignmentService
      .getPage({
        search: this.search || undefined,
        productId: this.productId,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        sortBy: this.sortBy,
        sortDirection: this.sortDirection || 'desc',
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.dataSource.data = response.items;
          this.totalCount.set(response.totalCount);
        },
        error: (error: HttpErrorResponse) =>
          this.loadError.set(
            error.error?.detail ?? 'Could not load assignment history. Please retry.',
          ),
      });
  }

  protected updateSearch(value: string): void {
    this.search = value.trim();
    this.resetAndLoad();
  }

  protected updateProduct(productId: number): void {
    this.productId = productId || undefined;
    this.resetAndLoad();
  }

  protected clearFilters(searchInput: HTMLInputElement): void {
    searchInput.value = '';
    this.search = '';
    this.productId = undefined;
    this.resetAndLoad();
  }

  protected changePage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadData();
  }

  protected changeSort(sort: Sort): void {
    if (!sort.direction) return;
    this.sortBy = sort.active === 'employeeName'
      ? 'employee'
      : sort.active === 'assignmentNumber'
        ? 'assignmentId'
        : sort.active;
    this.sortDirection = sort.direction;
    this.resetAndLoad();
  }

  protected openCreateDialog(): void {
    const dialogRef = this.dialog.open<
      AssignmentDialog,
      AssignmentDialogData,
      AssetAssignment | undefined
    >(AssignmentDialog, {
      width: '650px',
      maxWidth: 'calc(100vw - 28px)',
      maxHeight: 'calc(100vh - 32px)',
      autoFocus: 'first-tabbable',
      data: { mode: 'create' },
    });
    dialogRef.afterClosed().subscribe((assignment) => {
      if (!assignment) return;
      this.snackBar.open(
        `Assets assigned to ${assignment.employeeName}.`,
        'Dismiss',
        { duration: 4000 },
      );
      this.loadData();
    });
  }

  protected viewAssignment(row: AssetAssignment): void {
    this.assignmentService.getById(row.assignmentId).subscribe({
      next: (assignment) => {
        this.dialog.open<AssignmentDialog, AssignmentDialogData>(
          AssignmentDialog,
          {
            width: '650px',
            maxWidth: 'calc(100vw - 28px)',
            maxHeight: 'calc(100vh - 32px)',
            data: { mode: 'view', assignment },
          },
        );
      },
      error: (error: HttpErrorResponse) =>
        this.snackBar.open(
          error.error?.detail ?? 'Could not load assignment details. Please retry.',
          'Dismiss',
          { duration: 5000 },
        ),
    });
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.loadData();
  }
}
