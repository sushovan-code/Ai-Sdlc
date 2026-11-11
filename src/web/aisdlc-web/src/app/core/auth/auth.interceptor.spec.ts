import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { makeToken, seedSession } from '../../testing/fixtures';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let router: Router;

  function setUp(): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => controller.verify());

  it('adds the bearer token to API calls', () => {
    seedSession(makeToken());
    setUp();

    http.get('/api/v1/users').subscribe();

    const request = controller.expectOne('/api/v1/users');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({});
  });

  it('does not send the token to other origins or paths', () => {
    seedSession(makeToken());
    setUp();

    http.get('https://example.com/data').subscribe();

    const request = controller.expectOne('https://example.com/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('sends no Authorization header when signed out', () => {
    setUp();

    http.get('/api/v1/auth/me').subscribe({ error: () => undefined });

    const request = controller.expectOne('/api/v1/auth/me');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush('', { status: 401, statusText: 'Unauthorized' });
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('signs out and redirects to login when the server rejects the token', () => {
    seedSession(makeToken());
    setUp();
    const auth = TestBed.inject(AuthService);

    http.get('/api/v1/users').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/users').flush('', { status: 401, statusText: 'Unauthorized' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('leaves a failed sign-in alone so the form can show the error', () => {
    seedSession(makeToken());
    setUp();
    const auth = TestBed.inject(AuthService);
    let status = 0;

    http.post('/api/v1/auth/login', {}).subscribe({ error: (e) => (status = e.status) });
    controller
      .expectOne('/api/v1/auth/login')
      .flush('', { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not treat a 403 as an expired session', () => {
    seedSession(makeToken());
    setUp();
    const auth = TestBed.inject(AuthService);

    http.get('/api/v1/users').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/users').flush('', { status: 403, statusText: 'Forbidden' });

    expect(auth.isAuthenticated()).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
