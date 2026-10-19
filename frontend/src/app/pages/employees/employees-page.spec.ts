import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { Employee, EmployeePage } from './employee.models';
import { EmployeeService } from './employee.service';
import { EmployeesPage } from './employees-page';

describe('EmployeesPage', () => {
  let fixture: ComponentFixture<EmployeesPage>;
  let getPage: ReturnType<typeof vi.fn>;
  let dialogOpen: ReturnType<typeof vi.fn>;
  const employee: Employee = {
    employeeId: 1,
    employeeCode: 'E-1',
    employeeName: 'Alex Example',
    email: 'alex@example.com',
    department: 'IT',
    designation: 'Analyst',
    phone: null,
    location: null,
    joiningDate: null,
    isActive: true,
    createdBy: 'admin',
    createdOn: '2026-01-01T00:00:00Z',
    modifiedBy: null,
    modifiedOn: null,
  };
  const response: EmployeePage = {
    items: [employee],
    totalCount: 20,
    page: 1,
    pageSize: 10,
    departments: ['IT'],
  };

  beforeEach(async () => {
    getPage = vi.fn().mockReturnValue(of(response));
    dialogOpen = vi.fn().mockReturnValue({ afterClosed: () => of(undefined) });
    TestBed.configureTestingModule({
      imports: [EmployeesPage],
      providers: [
        {
          provide: EmployeeService,
          useValue: {
            getPage,
            getById: () => of(employee),
            setStatus: () => of({ ...employee, isActive: false }),
          },
        },
        { provide: AuthService, useValue: { currentUser: signal({ roles: ['Admin'] }) } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
      ],
    });
    TestBed.overrideProvider(MatDialog, { useValue: { open: dialogOpen } });
    await TestBed.compileComponents();
    fixture = TestBed.createComponent(EmployeesPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('loads employees from the API and displays employee information', () => {
    expect(getPage).toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Alex Example');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('alex@example.com');
  });

  it('opens the create employee dialog', () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('.create-button')!
      .click();

    expect(dialogOpen).toHaveBeenCalledWith(
      expect.any(Function),
      expect.objectContaining({
        data: expect.objectContaining({ mode: 'create' }),
      }),
    );
  });

  it('applies search input to the API query', () => {
    const input = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLInputElement>('input[matInput]')!;
    input.value = ' E-1 ';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(getPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'E-1', page: 1 }),
    );
  });

  it('requests the next page when the paginator advances', () => {
    const nextPage = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('button[aria-label="Next page"]')!;
    nextPage.click();
    fixture.detectChanges();

    expect(getPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2, pageSize: 10 }),
    );
  });
});
