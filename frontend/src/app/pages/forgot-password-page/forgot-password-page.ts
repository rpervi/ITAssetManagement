import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';

@Component({
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  selector: 'app-forgot-password-page',
  styleUrl: './forgot-password-page.css',
  templateUrl: './forgot-password-page.html',
})
export class ForgotPasswordPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });
  protected readonly loading = signal(false);
  protected readonly submitted = signal(false);
  protected readonly errorMessage = signal('');

  protected submit(): void {
    this.errorMessage.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.authService.requestPasswordReset(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitted.set(true);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('We could not process your request. Please try again later.');
        this.loading.set(false);
      },
    });
  }
}
