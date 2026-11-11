import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { click, host, textOf, typeInto } from '../../../testing/dom';
import { makeToken } from '../../../testing/fixtures';
import { LoginPage } from './login';

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  function fill(email: string, password: string): void {
    typeInto(host(fixture), '#email', email);
    typeInto(host(fixture), '#password', password);
  }

  it('shows the welcome heading and a link to create an account', () => {
    expect(textOf(host(fixture))).toContain('Welcome back');
    expect(host(fixture).querySelector('a[href="/register"]')).not.toBeNull();
  });

  it('does not call the API when the form is empty and tells the user what is missing', async () => {
    click(host(fixture), 'button[type="submit"]');
    await fixture.whenStable();

    http.expectNone('/api/v1/auth/login');
    expect(textOf(host(fixture))).toContain('Enter a valid email address.');
    expect(textOf(host(fixture))).toContain('Enter your password.');
  });

  it('signs in and goes to the home page', async () => {
    fill('pm@demo.local', 'Demo#Passw0rd!');
    click(host(fixture), 'button[type="submit"]');

    const request = http.expectOne('/api/v1/auth/login');
    expect(request.request.body).toEqual({ email: 'pm@demo.local', password: 'Demo#Passw0rd!' });
    request.flush(makeToken());
    await fixture.whenStable();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows the server message and stays on the page when the sign-in is rejected', async () => {
    fill('pm@demo.local', 'wrong');
    click(host(fixture), 'button[type="submit"]');

    http
      .expectOne('/api/v1/auth/login')
      .flush({ title: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain(
      'Invalid email or password.',
    );
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    const submit = host(fixture).querySelector<HTMLButtonElement>('button[type="submit"]');
    expect(submit?.disabled).toBe(false);
  });

  it('disables the button while the request is in flight', async () => {
    fill('pm@demo.local', 'Demo#Passw0rd!');
    click(host(fixture), 'button[type="submit"]');
    await fixture.whenStable();

    const submit = host(fixture).querySelector<HTMLButtonElement>('button[type="submit"]');
    expect(submit?.disabled).toBe(true);
    expect(textOf(submit as HTMLElement)).toContain('Signing in');
    http.expectOne('/api/v1/auth/login').flush(makeToken());
  });

  it('labels both fields for assistive technology', () => {
    expect(host(fixture).querySelector('label[for="email"]')?.textContent).toContain('Email');
    expect(host(fixture).querySelector('label[for="password"]')?.textContent).toContain('Password');
  });
});
