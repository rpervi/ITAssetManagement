import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { AssignmentService } from './assignment.service';
import { AssignmentsPage } from './assignments-page';

describe('AssignmentsPage', () => {
  let fixture: ComponentFixture<AssignmentsPage>;
  let getPage: ReturnType<typeof vi.fn>;
  let dialogOpen: ReturnType<typeof vi.fn>;
  const assignment = {
    assignmentId: 10,
    assignmentNumber: 'ASN-000010',
    employeeId: 2,
    employeeCode: 'E-1',
    employeeName: 'Alex Example',
    department: 'IT',
    assignedDate: '2026-10-19T00:00:00Z',
    assignedBy: 'it-staff',
    remarks: null,
    issuedQuantity: 1,
    status: 'Active',
    items: [],
  };

  beforeEach(async () => {
    getPage = vi.fn().mockReturnValue(of({
      items: [assignment],
      totalCount: 1,
      page: 1,
      pageSize: 25,
    }));
    dialogOpen = vi.fn().mockReturnValue({ afterClosed: () => of(undefined) });
    TestBed.configureTestingModule({
      imports: [AssignmentsPage],
      providers: [
        {
          provide: AssignmentService,
          useValue: {
            getPage,
            getActiveProducts: () => of([]),
            getById: () => of(assignment),
          },
        },
        { provide: AuthService, useValue: { currentUser: signal({ roles: ['ITStaff'] }) } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
      ],
    });
    TestBed.overrideProvider(MatDialog, { useValue: { open: dialogOpen } });
    await TestBed.compileComponents();
    fixture = TestBed.createComponent(AssignmentsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('loads assignment history from the API', () => {
    expect(getPage).toHaveBeenCalled();
    const content = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(content).toContain('ASN-000010');
    expect(content).toContain('Alex Example');
  });

  it('opens the create dialog for assignment staff', () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('.create-button')!
      .click();
    expect(dialogOpen).toHaveBeenCalledWith(
      expect.any(Function),
      expect.objectContaining({
        data: { mode: 'create' },
      }),
    );
  });

  it('sends typed search to the API', () => {
    const input = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLInputElement>('input[matInput]')!;
    input.value = 'Alex';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(getPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'Alex', page: 1 }),
    );
  });
});
