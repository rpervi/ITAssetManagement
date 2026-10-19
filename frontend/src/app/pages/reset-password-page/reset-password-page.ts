import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
  selector: 'app-reset-password-page',
  styleUrl: '../forgot-password-page/forgot-password-page.css',
  templateUrl: './reset-password-page.html',
})
export class ResetPasswordPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(12),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    {
      validators: (group) =>
        group.get('newPassword')?.value === group.get('confirmPassword')?.value
          ? null
          : { passwordMismatch: true },
    },
  );
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly resetComplete = signal(false);

  protected submit(): void {
    this.errorMessage.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!email || !token) {
      this.errorMessage.set('This reset link is invalid or has expired.');
      return;
    }

    this.loading.set(true);
    this.authService
      .resetPassword({ email, token, newPassword: this.form.controls.newPassword.value })
      .subscribe({
        next: () => {
          this.resetComplete.set(true);
          this.loading.set(false);
        },
        error: () => {
          this.errorMessage.set('This reset link is invalid or has expired. Please request a new one.');
          this.loading.set(false);
        },
      });
  }
}
