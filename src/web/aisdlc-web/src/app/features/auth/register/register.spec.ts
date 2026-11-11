import { toastMessages } from '../../../testing/toasts';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { click, host, textOf, typeInto } from '../../../testing/dom';
import { makeToken, makeUser } from '../../../testing/fixtures';
import { RegisterPage } from './register';

const STRONG_PASSWORD = 'Str0ng#Passw0rd';

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
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
    fixture = TestBed.createComponent(RegisterPage);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  function fill(password = STRONG_PASSWORD, confirm = STRONG_PASSWORD): void {
    const root = host(fixture);
    typeInto(root, '#displayName', 'New Person');
    typeInto(root, '#email', 'new@demo.local');
    typeInto(root, '#password', password);
    typeInto(root, '#confirmPassword', confirm);
  }

  it('explains that new accounts start as Developers', () => {
    expect(textOf(host(fixture))).toContain('New accounts start as Developers');
  });

  it('blocks a password that is too short without calling the API', async () => {
    fill('short', 'short');
    click(host(fixture), 'button[type="submit"]');
    await fixture.whenStable();

    http.expectNone('/api/v1/auth/register');
    expect(textOf(host(fixture))).toContain('Use at least 12 characters.');
  });

  it('blocks mismatched passwords', async () => {
    fill(STRONG_PASSWORD, 'Different#Passw0rd');
    click(host(fixture), 'button[type="submit"]');
    await fixture.whenStable();

    http.expectNone('/api/v1/auth/register');
    expect(textOf(host(fixture))).toContain('The passwords do not match.');
  });

  it('creates the account, signs in and goes to the home page', async () => {
    fill();
    click(host(fixture), 'button[type="submit"]');

    const register = http.expectOne('/api/v1/auth/register');
    expect(register.request.body).toEqual({
      displayName: 'New Person',
      email: 'new@demo.local',
      password: STRONG_PASSWORD,
    });
    register.flush(makeUser({ email: 'new@demo.local' }));
    http.expectOne('/api/v1/auth/login').flush(makeToken());
    await fixture.whenStable();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows the password policy messages returned by the server', async () => {
    fill();
    click(host(fixture), 'button[type="submit"]');

    http.expectOne('/api/v1/auth/register').flush(
      {
        title: 'One or more validation errors occurred.',
        errors: { password: ['Passwords must have at least one symbol.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Passwords must have at least one symbol.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('tells the user when the email is already registered', async () => {
    fill();
    click(host(fixture), 'button[type="submit"]');

    http
      .expectOne('/api/v1/auth/register')
      .flush(
        { title: 'An account with this email already exists.' },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    expect(toastMessages().join(' ')).toContain('An account with this email already exists.');
  });
});
