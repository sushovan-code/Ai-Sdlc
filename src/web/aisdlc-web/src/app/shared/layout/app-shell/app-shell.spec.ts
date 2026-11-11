import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { AppRole } from '../../../core/auth/roles';
import { click, host, textOf } from '../../../testing/dom';
import { makeToken, makeUser, seedSession } from '../../../testing/fixtures';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  let fixture: ComponentFixture<AppShell>;

  async function render(role: string, displayName = 'Demo Project Manager'): Promise<void> {
    sessionStorage.clear();
    seedSession(makeToken(makeUser({ role, displayName })));
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();
  }

  it('shows who is signed in with their initials and role', async () => {
    await render(AppRole.ProjectManager);

    const text = textOf(host(fixture));
    expect(text).toContain('Demo Project Manager');
    expect(text).toContain('Project Manager');
    expect(host(fixture).querySelector('.avatar')?.textContent?.trim()).toBe('DP');
  });

  it('offers the Users page to administrators only', async () => {
    await render(AppRole.Admin);

    expect(host(fixture).querySelector('a[href="/users"]')).not.toBeNull();
  });

  it.each([AppRole.ProjectManager, AppRole.Architect, AppRole.Developer, AppRole.QA])(
    'hides the Users page from the %s role',
    async (role) => {
      await render(role);

      expect(host(fixture).querySelector('a[href="/users"]')).toBeNull();
      expect(host(fixture).querySelector('a[href="/projects"]')).not.toBeNull();
    },
  );

  it('signs out and returns to the login page', async () => {
    await render(AppRole.Developer);
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    click(host(fixture), 'button.btn-sm:last-of-type');

    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('offers Light, Dark and System themes', async () => {
    await render(AppRole.QA);

    const labels = Array.from(host(fixture).querySelectorAll('aisdlc-theme-switcher button')).map(
      (b) => b.getAttribute('aria-label'),
    );
    expect(labels).toEqual(['Light', 'Dark', 'System']);
  });
});
