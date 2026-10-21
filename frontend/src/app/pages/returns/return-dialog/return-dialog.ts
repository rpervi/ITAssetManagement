import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { AssignmentItem, AssetAssignment, ReturnAssignmentRequest } from '../../assignments/assignment.models';
import { AssignmentService } from '../../assignments/assignment.service';

export interface ReturnDialogData {
  assignment: AssetAssignment;
  item: AssignmentItem;
}

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  selector: 'app-return-dialog',
  styleUrl: './return-dialog.css',
  templateUrl: './return-dialog.html',
})
export class ReturnDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assignmentService = inject(AssignmentService);
  private readonly dialogRef =
    inject<MatDialogRef<ReturnDialog, AssetAssignment | undefined>>(MatDialogRef);
  protected readonly data = inject<ReturnDialogData>(MAT_DIALOG_DATA);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly form = this.formBuilder.nonNullable.group({
    quantity: [
      1,
      [
        Validators.required,
        Validators.min(1),
        Validators.max(this.data.item.outstandingQuantity),
      ],
    ],
    returnDate: [this.localDateToday(), Validators.required],
    returnedCondition: ['Good', Validators.required],
    remarks: ['', Validators.maxLength(1000)],
  });

  protected submit(): void {
    this.errorMessage.set('');
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const request: ReturnAssignmentRequest = {
      assignmentItemId: this.data.item.assignmentItemId,
      quantity: values.quantity,
      returnDate: values.returnDate,
      returnedCondition: values.returnedCondition,
      remarks: values.remarks.trim() || null,
    };
    this.assignmentService
      .returnAssets(this.data.assignment.assignmentId, request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (assignment) => this.dialogRef.close(assignment),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.detail ?? 'Could not record this return. Please retry.',
          ),
      });
    this.submitting.set(true);
  }

  private localDateToday(): string {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }
}
