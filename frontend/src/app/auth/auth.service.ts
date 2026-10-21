import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, switchMap, tap, throwError } from 'rxjs';
import {
  ApiMessage,
  AuthenticatedUser,
  ForgotPasswordRequest,
  LoginRequest,
  ResetPasswordRequest,
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private csrfRequest?: Observable<void>;
  readonly currentUser = signal<AuthenticatedUser | null>(null);

  login(request: LoginRequest): Observable<AuthenticatedUser> {
    return this.ensureCsrfToken().pipe(
      switchMap(() =>
        this.http.post<AuthenticatedUser>('/api/auth/login', request, { withCredentials: true }),
      ),
      switchMap((user) => {
        this.currentUser.set(user);
        return this.refreshCsrfToken().pipe(map(() => user));
      }),
    );
  }

  getCurrentUser(): Observable<AuthenticatedUser | null> {
    return this.http
      .get<AuthenticatedUser>('/api/auth/current-user', { withCredentials: true })
      .pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status !== 401) {
            return throwError(() => error);
          }

          return this.ensureCsrfToken().pipe(
            switchMap(() =>
              this.http
                .post<AuthenticatedUser>(
                  '/api/auth/refresh',
                  {},
                  { withCredentials: true },
                )
                .pipe(
                  catchError((refreshError: HttpErrorResponse) =>
                    refreshError.status === 401 ? of(null) : throwError(() => refreshError),
                  ),
                ),
            ),
            switchMap((user) =>
              user ? this.refreshCsrfToken().pipe(map(() => user)) : of(null),
            ),
          );
        }),
        tap((user) => this.currentUser.set(user)),
      );
  }

  logout(): Observable<void> {
    return this.ensureCsrfToken().pipe(
      switchMap(() =>
        this.http.post<void>('/api/auth/logout', {}, { withCredentials: true }),
      ),
      tap(() => {
        this.currentUser.set(null);
        this.csrfRequest = undefined;
      }),
    );
  }

  requestPasswordReset(request: ForgotPasswordRequest): Observable<ApiMessage> {
    return this.ensureCsrfToken().pipe(
      switchMap(() =>
        this.http.post<ApiMessage>('/api/auth/forgot-password', request, {
          withCredentials: true,
        }),
      ),
    );
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.ensureCsrfToken().pipe(
      switchMap(() =>
        this.http.post<void>('/api/auth/reset-password', request, {
          withCredentials: true,
        }),
      ),
    );
  }

  private ensureCsrfToken(): Observable<void> {
    if (!this.csrfRequest) {
      this.csrfRequest = this.http
        .get<{ requestToken: string }>('/api/auth/csrf', { withCredentials: true })
        .pipe(
          map(() => void 0),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    return this.csrfRequest;
  }

  private refreshCsrfToken(): Observable<void> {
    this.csrfRequest = undefined;
    return this.ensureCsrfToken();
  }
}
