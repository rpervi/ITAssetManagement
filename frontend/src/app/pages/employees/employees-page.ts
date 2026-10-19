import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize, firstValueFrom, Subscription } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { Employee, EmployeePage, EmployeeQuery } from './employee.models';
import {
  EmployeeDialog,
  EmployeeDialogData,
  EmployeeDialogResult,
} from './employee-dialog/employee-dialog';
import { EmployeeService } from './employee.service';

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
  selector: 'app-employees-page',
  styleUrl: './employees-page.css',
  templateUrl: './employees-page.html',
})
export class EmployeesPage {
  private readonly employeeService = inject(EmployeeService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = [
    'employeeCode',
    'employeeName',
    'email',
    'department',
    'designation',
    'phone',
    'location',
    'status',
    'actions',
  ];
  protected readonly dataSource = new MatTableDataSource<Employee>([]);
  protected readonly departments = signal<string[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly loadError = signal('');
  protected readonly statusUpdating = signal<number | null>(null);
  protected readonly isAdmin = computed(
    () => this.authService.currentUser()?.roles.includes('Admin') ?? false,
  );
  private search = '';
  private department = '';
  private status = '';
  private sortBy = 'employeeName';
  private sortDirection: SortDirection = 'asc';
  private listRequest?: Subscription;

  constructor() {
    this.loadData();
  }

  protected loadData(): void {
    this.listRequest?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    this.listRequest = this.employeeService
      .getPage(this.query())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
      next: (response) => this.setPage(response),
      error: () => {
        this.loadError.set('Could not load employees. Please refresh and try again.');
      },
    });
  }

  protected updateSearch(value: string): void {
    this.search = value.trim();
    this.resetAndLoad();
  }

  protected updateDepartment(department: string): void {
    this.department = department;
    this.resetAndLoad();
  }

  protected updateStatus(status: string): void {
    this.status = status;
    this.resetAndLoad();
  }

  protected clearFilters(searchInput: HTMLInputElement): void {
    searchInput.value = '';
    this.search = '';
    this.department = '';
    this.status = '';
    this.resetAndLoad();
  }

  protected changePage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadData();
  }

  protected changeSort(sort: Sort): void {
    if (!sort.direction) return;
    this.sortBy = sort.active;
    this.sortDirection = sort.direction;
    this.resetAndLoad();
  }

  protected openDialog(mode: EmployeeDialogData['mode'], employee?: Employee): void {
    if (mode === 'create') {
      this.showDialog(mode);
      return;
    }

    this.employeeService.getById(employee!.employeeId).subscribe({
      next: (details) => this.showDialog(mode, details),
      error: (error: HttpErrorResponse) =>
        this.snackBar.open(
          error.error?.detail ?? 'Could not load employee details. Please try again.',
          'Dismiss',
          { duration: 5000 },
        ),
    });
  }

  protected toggleStatus(employee: Employee): void {
    const isActive = !employee.isActive;
    this.statusUpdating.set(employee.employeeId);
    this.employeeService.setStatus(employee.employeeId, isActive).subscribe({
      next: (updated) => {
        this.statusUpdating.set(null);
        this.snackBar.open(
          `Employee ${updated.isActive ? 'activated' : 'deactivated'}.`,
          'Dismiss',
          { duration: 3500 },
        );
        this.loadData();
      },
      error: (error: HttpErrorResponse) => {
        this.statusUpdating.set(null);
        this.snackBar.open(
          error.error?.detail ?? 'Could not update employee status. Please try again.',
          'Dismiss',
          { duration: 5000 },
        );
      },
    });
  }

  protected async exportExcel(): Promise<void> {
    this.exporting.set(true);
    const exportQuery = { ...this.query(), page: 1, pageSize: 100 };
    try {
      const firstPage = await firstValueFrom(this.employeeService.getPage(exportQuery));
      const employees = [...firstPage.items];
      const pageCount = Math.ceil(firstPage.totalCount / firstPage.pageSize);
      for (let page = 2; page <= pageCount; page++) {
        const response = await firstValueFrom(
          this.employeeService.getPage({
            ...exportQuery,
            page,
            pageSize: firstPage.pageSize,
          }),
        );
        employees.push(...response.items);
      }

      this.downloadExcel(employees);
    } catch {
      this.snackBar.open('Could not export employees. Please try again.', 'Dismiss', {
        duration: 5000,
      });
    } finally {
      this.exporting.set(false);
    }
  }

  private showDialog(mode: EmployeeDialogData['mode'], employee?: Employee): void {
    const dialogRef = this.dialog.open<
      EmployeeDialog,
      EmployeeDialogData,
      EmployeeDialogResult | undefined
    >(EmployeeDialog, {
      width: '680px',
      maxWidth: 'calc(100vw - 32px)',
      autoFocus: 'first-tabbable',
      data: { mode, employee },
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (!result) return;
      this.snackBar.open(
        result.action === 'created' ? 'Employee created.' : 'Employee updated.',
        'Dismiss',
        { duration: 3500 },
      );
      this.loadData();
    });
  }

  private query(): EmployeeQuery {
    return {
      search: this.search || undefined,
      department: this.department || undefined,
      isActive: this.status === '' ? undefined : this.status === 'active',
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      sortBy: this.sortBy,
      sortDirection: this.sortDirection || 'asc',
    };
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.loadData();
  }

  private setPage(response: EmployeePage): void {
    this.dataSource.data = response.items;
    this.totalCount.set(response.totalCount);
    this.departments.set(response.departments);
  }

  private downloadExcel(employees: Employee[]): void {
    const columns = [
      'Employee Code',
      'Employee Name',
      'Email',
      'Department',
      'Designation',
      'Phone',
      'Location',
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
    const cell = (value: string): string =>
      `<Cell><Data ss:Type="String">${escapeXml(value)}</Data></Cell>`;
    const rows = employees.map((employee) => [
      employee.employeeCode,
      employee.employeeName,
      employee.email,
      employee.department,
      employee.designation ?? '',
      employee.phone ?? '',
      employee.location ?? '',
      employee.isActive ? 'Active' : 'Inactive',
    ]);
    const workbook = [
      '<?xml version="1.0"?>',
      '<?mso-application progid="Excel.Sheet"?>',
      '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">',
      '<Worksheet ss:Name="Employees"><Table>',
      `<Row>${columns.map(cell).join('')}</Row>`,
      ...rows.map((row) => `<Row>${row.map(cell).join('')}</Row>`),
      '</Table></Worksheet></Workbook>',
    ].join('');
    const url = URL.createObjectURL(
      new Blob([workbook], { type: 'application/vnd.ms-excel;charset=utf-8' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = 'employees.xls';
    link.click();
    URL.revokeObjectURL(url);
  }
}
