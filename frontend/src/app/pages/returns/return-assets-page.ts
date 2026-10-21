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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize, Subscription } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { AssignmentItem, AssetAssignment } from '../assignments/assignment.models';
import { AssignmentService } from '../assignments/assignment.service';
import { ReturnDialog, ReturnDialogData } from './return-dialog/return-dialog';

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
    MatSnackBarModule,
  ],
  selector: 'app-return-assets-page',
  styleUrl: './return-assets-page.css',
  templateUrl: './return-assets-page.html',
})
export class ReturnAssetsPage {
  private readonly assignmentService = inject(AssignmentService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly assignments = signal<AssetAssignment[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly canReturnAssets = computed(() => {
    const roles = this.authService.currentUser()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('ITStaff');
  });
  private search = '';
  private listRequest?: Subscription;

  constructor() {
    this.loadData();
  }

  protected loadData(): void {
    this.listRequest?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    this.listRequest = this.assignmentService
      .getPage({
        search: this.search || undefined,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        sortBy: 'assignedDate',
        sortDirection: 'desc',
        returnableOnly: true,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.assignments.set(response.items);
          this.totalCount.set(response.totalCount);
        },
        error: (error: HttpErrorResponse) =>
          this.loadError.set(
            error.error?.detail ?? 'Could not load outstanding asset assignments. Please retry.',
          ),
      });
  }

  protected updateSearch(value: string): void {
    this.search = value.trim();
    this.pageIndex.set(0);
    this.loadData();
  }

  protected changePage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadData();
  }

  protected openReturnDialog(assignment: AssetAssignment, item: AssignmentItem): void {
    const dialogRef = this.dialog.open<
      ReturnDialog,
      ReturnDialogData,
      AssetAssignment | undefined
    >(ReturnDialog, {
      width: '540px',
      maxWidth: 'calc(100vw - 28px)',
      maxHeight: 'calc(100vh - 32px)',
      autoFocus: 'first-tabbable',
      data: { assignment, item },
    });
    dialogRef.afterClosed().subscribe((updatedAssignment) => {
      if (!updatedAssignment) return;
      this.snackBar.open(
        `Return recorded for ${updatedAssignment.employeeName}.`,
        'Dismiss',
        { duration: 4000 },
      );
      this.loadData();
    });
  }

  protected outstandingItems(assignment: AssetAssignment): AssignmentItem[] {
    return assignment.items.filter((item) => item.outstandingQuantity > 0);
  }
}
