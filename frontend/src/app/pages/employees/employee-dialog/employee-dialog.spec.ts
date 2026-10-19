import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { EmployeeService } from '../employee.service';
import { EmployeeDialog } from './employee-dialog';

describe('EmployeeDialog', () => {
  let fixture: ComponentFixture<EmployeeDialog>;
  let createEmployee: ReturnType<typeof vi.fn>;
  let closeDialog: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    createEmployee = vi.fn().mockReturnValue(of({ employeeId: 1 }));
    closeDialog = vi.fn();
    TestBed.configureTestingModule({
      imports: [EmployeeDialog],
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { mode: 'create' },
        },
        { provide: MatDialogRef, useValue: { close: closeDialog } },
        { provide: EmployeeService, useValue: { create: createEmployee } },
      ],
    });
    await TestBed.compileComponents();
    fixture = TestBed.createComponent(EmployeeDialog);
    fixture.detectChanges();
  });

  it('shows required-field errors when submitted empty', () => {
    const requiredFields = ['employeeCode', 'employeeName', 'email', 'department'];
    for (const field of requiredFields) {
      const input = (fixture.nativeElement as HTMLElement)
        .querySelector<HTMLInputElement>(`[formControlName="${field}"]`)!;
      input.dispatchEvent(new Event('blur'));
    }
    fixture.detectChanges();

    const content = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(content).toContain('Employee code is required.');
    expect(content).toContain('Employee name is required.');
    expect(content).toContain('Email is required.');
    expect(content).toContain('Department is required.');
  });

  it('rejects a future joining date', () => {
    const input = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLInputElement>('input[formControlName="joiningDate"]')!;
    input.value = '2999-01-01';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Joining date cannot be in the future.');
  });

  it('saves a valid employee and closes after success', () => {
    fillRequiredFields();
    fixture.detectChanges();
    const submit = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('button[type="submit"]')!;
    expect(submit.disabled).toBe(false);
    submit.click();
    fixture.detectChanges();

    expect(createEmployee).toHaveBeenCalledWith(
      expect.objectContaining({
        employeeCode: 'E-1',
        employeeName: 'Alex Example',
        email: 'alex@example.com',
        department: 'IT',
      }),
    );
    expect(closeDialog).toHaveBeenCalledWith(
      expect.objectContaining({ action: 'created' }),
    );
  });

  it('shows API errors without clearing entered values', () => {
    createEmployee.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { detail: 'An employee with this email already exists.' },
      })),
    );
    fillRequiredFields();
    fixture.detectChanges();
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('button[type="submit"]')!
      .click();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('An employee with this email already exists.');
    expect(root.querySelector<HTMLInputElement>('[formControlName="employeeCode"]')?.value)
      .toBe('E-1');
    expect(root.querySelector<HTMLInputElement>('[formControlName="email"]')?.value)
      .toBe('alex@example.com');
    expect(closeDialog).not.toHaveBeenCalled();
  });

  function fillRequiredFields(): void {
    const values: Record<string, string> = {
      employeeCode: 'E-1',
      employeeName: 'Alex Example',
      email: 'alex@example.com',
      department: 'IT',
    };
    const root = fixture.nativeElement as HTMLElement;
    for (const [field, value] of Object.entries(values)) {
      const input = root.querySelector<HTMLInputElement>(`[formControlName="${field}"]`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
  }
});
