import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { EmployeeOption } from '../../employees/employee.models';
import { EmployeeService } from '../../employees/employee.service';
import {
  AssignmentProduct,
  AssetAssignment,
  AvailableAssignmentStock,
  AvailableAssignmentUnit,
  CreateAssignmentRequest,
} from '../assignment.models';
import { AssignmentService } from '../assignment.service';

export interface AssignmentDialogData {
  mode: 'create' | 'view';
  assignment?: AssetAssignment;
}

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  selector: 'app-assignment-dialog',
  styleUrl: './assignment-dialog.css',
  templateUrl: './assignment-dialog.html',
})
export class AssignmentDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly assignmentService = inject(AssignmentService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialogRef =
    inject<MatDialogRef<AssignmentDialog, AssetAssignment | undefined>>(MatDialogRef);
  protected readonly data = inject<AssignmentDialogData>(MAT_DIALOG_DATA);
  protected readonly employees = signal<EmployeeOption[]>([]);
  protected readonly products = signal<AssignmentProduct[]>([]);
  protected readonly units = signal<AvailableAssignmentUnit[]>([]);
  protected readonly bulkStock = signal<AvailableAssignmentStock | null>(null);
  protected readonly loadingOptions = signal(false);
  protected readonly loadingAvailability = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly availabilityError = signal('');
  protected readonly selectedUnitIds = signal<number[]>([]);
  private pendingIdempotencyKey = '';
  private pendingPayloadSignature = '';

  protected readonly form = this.formBuilder.nonNullable.group({
    employeeId: [0, [Validators.required, Validators.min(1)]],
    productId: [0, [Validators.required, Validators.min(1)]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    assignedDate: [new Date().toISOString().slice(0, 10), Validators.required],
    remarks: ['', Validators.maxLength(1000)],
  });

  constructor() {
    if (this.data.mode === 'view') return;
    this.loadingOptions.set(true);
    this.employeeService.getActive().subscribe({
      next: (employees) => this.employees.set(employees),
      error: (error: HttpErrorResponse) =>
        this.errorMessage.set(
          error.error?.detail ?? 'Could not load active employees. Please retry.',
        ),
    });
    this.assignmentService
      .getActiveProducts()
      .pipe(finalize(() => this.loadingOptions.set(false)))
      .subscribe({
        next: (products) => this.products.set(products),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.detail ?? 'Could not load active products. Please retry.',
          ),
      });

    this.form.controls.productId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((productId) => this.loadAvailability(productId));
  }

  protected get selectedProduct(): AssignmentProduct | undefined {
    return this.products().find(
      (product) => product.productId === this.form.controls.productId.value,
    );
  }

  protected get isIndividual(): boolean {
    return this.selectedProduct?.trackingType === 'Individual';
  }

  protected get bulkQuantityExceedsAvailability(): boolean {
    const available = this.bulkStock()?.availableQuantity;
    return available !== undefined && this.form.controls.quantity.value > available;
  }

  protected title(): string {
    return this.data.mode === 'view' ? 'Assignment Details' : 'Assign Assets';
  }

  protected isUnitSelected(unitId: number): boolean {
    return this.selectedUnitIds().includes(unitId);
  }

  protected toggleUnit(unitId: number, selected: boolean): void {
    const unitIds = new Set(this.selectedUnitIds());
    if (selected) unitIds.add(unitId);
    else unitIds.delete(unitId);
    this.selectedUnitIds.set([...unitIds]);
  }

  protected submit(): void {
    this.errorMessage.set('');
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const product = this.selectedProduct;
    if (!product) {
      this.errorMessage.set('Select an active product.');
      return;
    }

    const unitIds = this.selectedUnitIds();
    if (product.trackingType === 'Individual' && unitIds.length === 0) {
      this.errorMessage.set('Select at least one available asset.');
      return;
    }
    if (product.trackingType === 'Bulk') {
      const stock = this.bulkStock();
      if (!stock || this.loadingAvailability()) {
        this.errorMessage.set('Wait for available stock to finish loading before assigning.');
        return;
      }

      if (values.quantity > stock.availableQuantity) {
        this.errorMessage.set(
          `Only ${stock.availableQuantity} unit(s) are available for assignment.`,
        );
        return;
      }
    }

    const payload: Omit<CreateAssignmentRequest, 'idempotencyKey'> = {
      employeeId: values.employeeId,
      productId: values.productId,
      assetUnitIds: product.trackingType === 'Individual' ? unitIds : null,
      quantity: product.trackingType === 'Individual' ? unitIds.length : values.quantity,
      assignedDate: values.assignedDate,
      remarks: values.remarks.trim() || null,
    };
    const signature = JSON.stringify(payload);
    if (this.pendingPayloadSignature !== signature) {
      this.pendingIdempotencyKey = crypto.randomUUID();
      this.pendingPayloadSignature = signature;
    }

    this.submitting.set(true);
    this.assignmentService
      .create({ ...payload, idempotencyKey: this.pendingIdempotencyKey })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (assignment) => this.dialogRef.close(assignment),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.detail ?? 'Could not create the assignment. Please try again.',
          ),
      });
  }

  private loadAvailability(productId: number): void {
    this.units.set([]);
    this.bulkStock.set(null);
    this.selectedUnitIds.set([]);
    this.availabilityError.set('');
    if (!productId) return;

    const product = this.products().find(item => item.productId === productId);
    if (!product) return;
    this.loadingAvailability.set(true);
    if (product.trackingType === 'Individual') {
      this.assignmentService
        .getAvailableUnits(productId)
        .pipe(finalize(() => this.loadingAvailability.set(false)))
        .subscribe({
          next: (units) => this.units.set(units),
          error: (error: HttpErrorResponse) =>
            this.availabilityError.set(
              error.error?.detail ?? 'Could not load available assets. Please retry.',
            ),
        });
      return;
    }

    this.assignmentService
      .getAvailableStock(productId)
      .pipe(finalize(() => this.loadingAvailability.set(false)))
      .subscribe({
        next: (stock) => this.bulkStock.set(stock),
        error: (error: HttpErrorResponse) =>
          this.availabilityError.set(
            error.error?.detail ?? 'Could not load available stock. Please retry.',
          ),
      });
  }
}
