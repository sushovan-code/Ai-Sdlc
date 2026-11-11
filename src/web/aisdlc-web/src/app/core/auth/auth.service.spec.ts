import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { makeToken, makeUser, seedSession } from '../../testing/fixtures';
import { AuthService } from './auth.service';
import { AppRole } from './roles';

describe('AuthService', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('starts signed out when nothing is stored', () => {
    const auth = TestBed.inject(AuthService);

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.user()).toBeNull();
    expect(auth.accessToken()).toBeNull();
  });

  it('login stores the session and exposes the user and role', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const token = makeToken(makeUser({ role: AppRole.Architect }));
    let signedInAs = '';

    auth.login('arch@demo.local', 'pw').subscribe((user) => (signedInAs = user.role));
    const request = http.expectOne('/api/v1/auth/login');
    expect(request.request.body).toEqual({ email: 'arch@demo.local', password: 'pw' });
    request.flush(token);

    expect(signedInAs).toBe(AppRole.Architect);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.role()).toBe(AppRole.Architect);
    expect(auth.accessToken()).toBe('test-token');
    expect(sessionStorage.getItem('aisdlc.session')).toContain('test-token');
  });

  it('register creates the account and then signs in with the same credentials', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth
      .register({ email: 'new@demo.local', displayName: 'New', password: 'Str0ng#Passw0rd' })
      .subscribe();
    http.expectOne('/api/v1/auth/register').flush(makeUser({ email: 'new@demo.local' }));
    const login = http.expectOne('/api/v1/auth/login');
    expect(login.request.body).toEqual({ email: 'new@demo.local', password: 'Str0ng#Passw0rd' });
    login.flush(makeToken());

    expect(auth.isAuthenticated()).toBe(true);
  });

  it('logout clears the signals and the stored session', () => {
    seedSession(makeToken());
    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(true);

    auth.logout();

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('aisdlc.session')).toBeNull();
  });

  it('restores a stored session that has not expired', () => {
    seedSession(makeToken(makeUser({ displayName: 'Restored' })));

    expect(TestBed.inject(AuthService).user()?.displayName).toBe('Restored');
  });

  it('ignores and removes a stored session that has expired', () => {
    seedSession(makeToken(makeUser(), -1000));

    const auth = TestBed.inject(AuthService);

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('aisdlc.session')).toBeNull();
  });

  it('drops the session when the token expires while the app is open', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    seedSession(makeToken(makeUser(), 60_000));
    const auth = TestBed.inject(AuthService);
    expect(auth.accessToken()).toBe('test-token');

    vi.advanceTimersByTime(61_000);

    expect(auth.accessToken()).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);
    vi.useRealTimers();
  });

  it('hasAnyRole is true only for the signed-in role', () => {
    seedSession(makeToken(makeUser({ role: AppRole.QA })));
    const auth = TestBed.inject(AuthService);

    expect(auth.hasAnyRole([AppRole.QA, AppRole.Admin])).toBe(true);
    expect(auth.hasAnyRole([AppRole.Admin])).toBe(false);
  });

  it('hasAnyRole is false when signed out', () => {
    expect(TestBed.inject(AuthService).hasAnyRole([AppRole.Admin])).toBe(false);
  });
});
