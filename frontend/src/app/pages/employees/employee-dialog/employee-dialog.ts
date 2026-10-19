import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { finalize } from 'rxjs';
import { Employee, SaveEmployeeRequest } from '../employee.models';
import { EmployeeService } from '../employee.service';

function notFutureDate(control: AbstractControl<string>): ValidationErrors | null {
  const date = control.value;
  return date && date > new Date().toISOString().slice(0, 10)
    ? { futureDate: true }
    : null;
}

export interface EmployeeDialogData {
  mode: 'create' | 'edit' | 'view';
  employee?: Employee;
}

export interface EmployeeDialogResult {
  action: 'created' | 'updated';
  employee: Employee;
}

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    ReactiveFormsModule,
  ],
  selector: 'app-employee-dialog',
  styleUrl: './employee-dialog.css',
  templateUrl: './employee-dialog.html',
})
export class EmployeeDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly dialogRef =
    inject<MatDialogRef<EmployeeDialog, EmployeeDialogResult | undefined>>(MatDialogRef);
  protected readonly data = inject<EmployeeDialogData>(MAT_DIALOG_DATA);
  protected readonly isView = this.data.mode === 'view';
  protected readonly isEdit = this.data.mode === 'edit';
  protected submitting = false;
  protected errorMessage = '';

  protected readonly form = this.formBuilder.nonNullable.group({
    employeeCode: [
      this.data.employee?.employeeCode ?? '',
      [Validators.required, Validators.maxLength(50)],
    ],
    employeeName: [
      this.data.employee?.employeeName ?? '',
      [Validators.required, Validators.maxLength(150)],
    ],
    email: [
      this.data.employee?.email ?? '',
      [Validators.required, Validators.email, Validators.maxLength(254)],
    ],
    department: [
      this.data.employee?.department ?? '',
      [Validators.required, Validators.maxLength(100)],
    ],
    designation: [this.data.employee?.designation ?? '', [Validators.maxLength(100)]],
    phone: [this.data.employee?.phone ?? '', [Validators.maxLength(30)]],
    location: [this.data.employee?.location ?? '', [Validators.maxLength(150)]],
    joiningDate: [
      this.data.employee?.joiningDate?.slice(0, 10) ?? '',
      [notFutureDate],
    ],
    isActive: [this.data.employee?.isActive ?? true],
  });

  constructor() {
    if (this.isView) this.form.disable();
  }

  protected title(): string {
    if (this.isView) return 'Employee Details';
    return this.isEdit ? 'Edit Employee' : 'Create Employee';
  }

  protected submit(): void {
    this.errorMessage = '';
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const request: SaveEmployeeRequest = {
      employeeCode: values.employeeCode.trim(),
      employeeName: values.employeeName.trim(),
      email: values.email.trim(),
      department: values.department.trim(),
      designation: values.designation.trim() || null,
      phone: values.phone.trim() || null,
      location: values.location.trim() || null,
      joiningDate: values.joiningDate || null,
      isActive: values.isActive,
    };
    if (
      !request.employeeCode
      || !request.employeeName
      || !request.email
      || !request.department
    ) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const operation = this.isEdit
      ? this.employeeService.update(this.data.employee!.employeeId, request)
      : this.employeeService.create(request);
    operation.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: (employee) =>
        this.dialogRef.close({
          action: this.isEdit ? 'updated' : 'created',
          employee,
        }),
      error: (error: HttpErrorResponse) => {
        const validationMessages = Object.values(error.error?.errors ?? {})
          .flat()
          .filter((message): message is string => typeof message === 'string')
          .join(' ');
        this.errorMessage =
          error.error?.detail
          ?? (validationMessages || error.error?.title || 'Could not save this employee. Please try again.');
      },
    });
  }
}
