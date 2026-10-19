import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let currentUser: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    currentUser = vi.fn().mockReturnValue(of(null));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { getCurrentUser: currentUser } },
      ],
    });
  });

  it('redirects anonymous visitors to login and preserves the requested URL', async () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/inventory' } as RouterStateSnapshot),
    );
    const redirect = await firstValueFrom(result as Observable<boolean | UrlTree>);

    expect(redirect).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(redirect as UrlTree)).toBe(
      '/login?returnUrl=%2Finventory',
    );
  });

  it('allows an authenticated visitor', async () => {
    currentUser.mockReturnValue(of({ userId: 1, username: 'itadmin' }));

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/inventory' } as RouterStateSnapshot),
    );

    await expect(firstValueFrom(result as Observable<boolean | UrlTree>)).resolves.toBe(true);
  });
});
