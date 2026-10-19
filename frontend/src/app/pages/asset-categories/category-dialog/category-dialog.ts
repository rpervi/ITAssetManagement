import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { finalize } from 'rxjs';
import { AssetCategoryService } from '../asset-category.service';
import { AssetCategory, SaveAssetCategoryRequest } from '../asset-category.models';

export interface CategoryDialogData {
  mode: 'create' | 'edit' | 'view';
  category?: AssetCategory;
}

export interface CategoryDialogResult {
  action: 'created' | 'updated';
  category: AssetCategory;
}

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    ReactiveFormsModule,
  ],
  selector: 'app-category-dialog',
  styleUrl: './category-dialog.css',
  templateUrl: './category-dialog.html',
})
export class CategoryDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly categoryService = inject(AssetCategoryService);
  private readonly dialogRef =
    inject<MatDialogRef<CategoryDialog, CategoryDialogResult | undefined>>(MatDialogRef);
  protected readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA);
  protected readonly isView = this.data.mode === 'view';
  protected readonly isEdit = this.data.mode === 'edit';
  protected submitting = false;
  protected errorMessage = '';

  protected readonly form = this.formBuilder.nonNullable.group({
    categoryName: [
      this.data.category?.categoryName ?? '',
      [Validators.required, Validators.maxLength(100)],
    ],
    description: [this.data.category?.description ?? '', [Validators.maxLength(500)]],
    isActive: [this.data.category?.isActive ?? true],
  });

  constructor() {
    if (this.isView) {
      this.form.disable();
    }
  }

  protected title(): string {
    if (this.isView) return 'Category details';
    return this.isEdit ? 'Edit category' : 'Create new category';
  }

  protected submit(): void {
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: SaveAssetCategoryRequest = {
      ...this.form.getRawValue(),
      categoryName: this.form.controls.categoryName.value.trim(),
      description: this.form.controls.description.value.trim() || null,
    };
    if (!request.categoryName) {
      this.form.controls.categoryName.setErrors({ required: true });
      this.form.controls.categoryName.markAsTouched();
      return;
    }

    this.submitting = true;
    const operation = this.isEdit
      ? this.categoryService.update(this.data.category!.categoryId, request)
      : this.categoryService.create(request);
    operation.pipe(finalize(() => (this.submitting = false))).subscribe({
      next: (category) =>
        this.dialogRef.close({
          action: this.isEdit ? 'updated' : 'created',
          category,
        }),
      error: (error: HttpErrorResponse) => {
        const validationMessages = Object.values(error.error?.errors ?? {})
          .flat()
          .filter((message): message is string => typeof message === 'string')
          .join(' ');
        this.errorMessage =
          error.status === 409
            ? (error.error?.detail ?? 'A category with this name already exists.')
            : (error.error?.detail ??
              (validationMessages ||
                error.error?.title ||
                'Could not save this category. Please try again.'));
      },
    });
  }
}
