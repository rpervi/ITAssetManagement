import { DatePipe } from '@angular/common';
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
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import {
  CategoryDialog,
  CategoryDialogData,
  CategoryDialogResult,
} from './category-dialog/category-dialog';
import { AssetCategory } from './asset-category.models';
import { AssetCategoryService } from './asset-category.service';
import { downloadCsv } from '../../shared/csv-export';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
  ],
  selector: 'app-asset-categories-page',
  styleUrl: './asset-categories-page.css',
  templateUrl: './asset-categories-page.html',
})
export class AssetCategoriesPage implements AfterViewInit {
  private readonly categoryService = inject(AssetCategoryService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild(MatSort) private sort!: MatSort;
  @ViewChild(MatPaginator) private paginator!: MatPaginator;

  protected readonly displayedColumns = [
    'categoryName',
    'description',
    'status',
    'createdOn',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<AssetCategory>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal('');
  protected readonly statusUpdating = signal<number | null>(null);

  constructor() {
    this.dataSource.filterPredicate = (category, filter) =>
      `${category.categoryName} ${category.description ?? ''} ${category.isActive ? 'active' : 'inactive'}`
        .toLowerCase()
        .includes(filter);
    this.loadCategories();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (category, property) => {
      switch (property) {
        case 'categoryName':
          return category.categoryName.toLowerCase();
        case 'description':
          return category.description?.toLowerCase() ?? '';
        case 'status':
          return category.isActive ? 1 : 0;
        case 'createdOn':
          return new Date(category.createdOn).getTime();
        default:
          return '';
      }
    };
  }

  protected applySearch(value: string): void {
    this.dataSource.filter = value.trim().toLowerCase();
    this.dataSource.paginator?.firstPage();
  }

  protected exportCsv(): void {
    const rows = this.dataSource.filteredData.map((category) => [
      category.categoryId,
      category.categoryName,
      category.description ?? '',
      category.isActive ? 'Active' : 'Inactive',
      category.createdOn,
    ]);
    downloadCsv(
      'asset-categories.csv',
      ['Category ID', 'Category Name', 'Description', 'Status', 'Created On'],
      rows,
    );
  }

  protected loadCategories(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.categoryService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (categories) => (this.dataSource.data = categories),
        error: () => this.loadError.set('Could not load categories. Please refresh and try again.'),
      });
  }

  protected openDialog(mode: CategoryDialogData['mode'], category?: AssetCategory): void {
    const dialogRef = this.dialog.open<
      CategoryDialog,
      CategoryDialogData,
      CategoryDialogResult | undefined
    >(CategoryDialog, {
      width: '520px',
      maxWidth: 'calc(100vw - 32px)',
      autoFocus: 'first-tabbable',
      data: { mode, category },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;
      this.snackBar.open(
        result.action === 'created' ? 'Category created.' : 'Category updated.',
        'Dismiss',
        { duration: 3500 },
      );
      this.loadCategories();
    });
  }

  protected toggleStatus(category: AssetCategory): void {
    const isActive = !category.isActive;
    this.statusUpdating.set(category.categoryId);
    this.categoryService
      .setStatus(category.categoryId, isActive)
      .pipe(finalize(() => this.statusUpdating.set(null)))
      .subscribe({
        next: (updated) => {
          this.dataSource.data = this.dataSource.data.map((item) =>
            item.categoryId === updated.categoryId ? updated : item,
          );
          this.snackBar.open(
            `Category ${updated.isActive ? 'activated' : 'deactivated'}.`,
            'Dismiss',
            { duration: 3500 },
          );
        },
        error: (error: HttpErrorResponse) => {
          this.snackBar.open(
            error.error?.detail ?? 'Could not update category status. Please try again.',
            'Dismiss',
            { duration: 5000 },
          );
        },
      });
  }
}
