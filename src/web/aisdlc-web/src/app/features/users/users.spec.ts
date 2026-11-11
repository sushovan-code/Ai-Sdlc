import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserDto } from '../../core/api/api-types';
import { AppRole } from '../../core/auth/roles';
import { click, host, textOf } from '../../testing/dom';
import { makeToken, makeUser, seedSession } from '../../testing/fixtures';
import { UsersPage } from './users';

const ADMIN = makeUser({
  id: 'aaaaaaaa-0000-0000-0000-000000000001',
  email: 'admin@demo.local',
  displayName: 'Demo Administrator',
  role: AppRole.Admin,
});
const DEV = makeUser({
  id: 'dddddddd-0000-0000-0000-000000000002',
  email: 'dev@demo.local',
  displayName: 'Demo Developer',
  role: AppRole.Developer,
});

function listResponse(items: UserDto[], totalCount = items.length) {
  return { items, page: '1', pageSize: '20', totalCount: String(totalCount) };
}

describe('UsersPage', () => {
  let fixture: ComponentFixture<UsersPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    seedSession(makeToken(ADMIN));
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(UsersPage);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  async function loadUsers(items = [ADMIN, DEV], total = items.length): Promise<void> {
    const request = http.expectOne((r) => r.url === '/api/v1/users');
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe('20');
    request.flush(listResponse(items, total));
    await fixture.whenStable();
  }

  it('shows a loading state, then the users with their roles and status', async () => {
    expect(textOf(host(fixture))).toContain('Loading users');

    await loadUsers();

    const text = textOf(host(fixture));
    expect(text).toContain('Demo Developer');
    expect(text).toContain('dev@demo.local');
    expect(text).toContain('2 users');
    expect(text).toContain('Active');
  });

  it('marks the signed-in administrator and stops them editing themselves', async () => {
    await loadUsers();

    const select = host(fixture).querySelector<HTMLSelectElement>(`#role-${ADMIN.id}`);
    expect(select?.disabled).toBe(true);
    expect(textOf(host(fixture))).toContain('You');
    const rows = host(fixture).querySelectorAll('tbody tr');
    expect(rows[0].querySelector<HTMLButtonElement>('button')?.disabled).toBe(true);
    expect(rows[1].querySelector<HTMLButtonElement>('button')?.disabled).toBe(false);
  });

  it('changes a role through the API and confirms it', async () => {
    await loadUsers();
    const select = host(fixture).querySelector<HTMLSelectElement>(
      `#role-${DEV.id}`,
    ) as HTMLSelectElement;

    select.value = AppRole.QA;
    select.dispatchEvent(new Event('change', { bubbles: true }));

    const request = http.expectOne(`/api/v1/users/${DEV.id}/role`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ role: AppRole.QA });
    request.flush({ ...DEV, role: AppRole.QA });
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Demo Developer is now QA.');
  });

  it('deactivates a user and offers reactivation', async () => {
    await loadUsers();

    click(host(fixture), 'tbody tr:nth-child(2) button');
    const request = http.expectOne(`/api/v1/users/${DEV.id}/deactivate`);
    expect(request.request.method).toBe('POST');
    request.flush({ ...DEV, isActive: false });
    await fixture.whenStable();

    const row = textOf(host(fixture).querySelectorAll('tbody tr')[1] as HTMLElement);
    expect(row).toContain('Deactivated');
    expect(row).toContain('Reactivate');
  });

  it('shows the error and reloads the list when a change is rejected', async () => {
    await loadUsers();
    const select = host(fixture).querySelector<HTMLSelectElement>(
      `#role-${DEV.id}`,
    ) as HTMLSelectElement;

    select.value = AppRole.Admin;
    select.dispatchEvent(new Event('change', { bubbles: true }));
    http
      .expectOne(`/api/v1/users/${DEV.id}/role`)
      .flush({ title: 'User not found.' }, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('User not found.');
    http.expectOne((r) => r.url === '/api/v1/users').flush(listResponse([ADMIN]));
  });

  it('pages through the list', async () => {
    await loadUsers([ADMIN, DEV], 45);
    expect(textOf(host(fixture))).toContain('Page 1 of 3');

    const next = Array.from(
      host(fixture).querySelectorAll<HTMLButtonElement>('footer button'),
    ).find((b) => b.textContent?.includes('Next')) as HTMLButtonElement;
    next.click();

    const request = http.expectOne((r) => r.url === '/api/v1/users');
    expect(request.request.params.get('page')).toBe('2');
    request.flush({ items: [DEV], page: '2', pageSize: '20', totalCount: '45' });
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Page 2 of 3');
  });

  it('shows a message when the list cannot be loaded', async () => {
    http
      .expectOne((r) => r.url === '/api/v1/users')
      .flush({ title: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
    await fixture.whenStable();

    expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('Forbidden');
    expect(textOf(host(fixture))).toContain('No users found.');
  });
});
