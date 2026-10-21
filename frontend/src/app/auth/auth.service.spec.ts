import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { AuthenticatedUser } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;

  const user: AuthenticatedUser = {
    userId: 7,
    username: 'test-admin',
    email: 'test-admin@example.test',
    roles: ['Admin'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('refreshes the antiforgery token after login and invalidates it on logout', () => {
    service.login({ username: user.username, password: 'password' }).subscribe();
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'anonymous-token' });
    httpTesting.expectOne('/api/auth/login').flush(user);
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'authenticated-token' });

    service.logout().subscribe();
    httpTesting.expectOne('/api/auth/logout').flush(null);

    service.login({ username: user.username, password: 'password' }).subscribe();
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'anonymous-token-2' });
    httpTesting.expectOne('/api/auth/login').flush(user);
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'authenticated-token-2' });
  });

  it('refreshes the antiforgery token after refreshing an expired session', () => {
    service.getCurrentUser().subscribe((currentUser) => expect(currentUser).toEqual(user));
    httpTesting.expectOne('/api/auth/current-user').flush(
      {},
      { status: 401, statusText: 'Unauthorized' },
    );
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'anonymous-token' });
    httpTesting.expectOne('/api/auth/refresh').flush(user);
    httpTesting.expectOne('/api/auth/csrf').flush({ requestToken: 'authenticated-token' });
    expect(service.currentUser()).toEqual(user);
  });
});
