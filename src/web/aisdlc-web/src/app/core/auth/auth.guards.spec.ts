import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { makeToken, makeUser, seedSession } from '../../testing/fixtures';
import { authGuard, guestGuard, roleGuard } from './auth.guards';
import { AppRole } from './roles';

describe('route guards', () => {
  const route = {} as ActivatedRouteSnapshot;

  function run(guard: CanActivateFn, url = '/projects'): boolean | UrlTree {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => guard(route, state)) as boolean | UrlTree;
  }

  function serialise(result: boolean | UrlTree): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  describe('authGuard', () => {
    it('lets a signed-in user through', () => {
      seedSession(makeToken());

      expect(run(authGuard)).toBe(true);
    });

    it('sends a signed-out visitor to login and remembers where they were going', () => {
      const result = run(authGuard, '/users');

      expect(serialise(result)).toBe('/login?returnUrl=%2Fusers');
    });
  });

  describe('guestGuard', () => {
    it('lets a signed-out visitor see the login page', () => {
      expect(run(guestGuard)).toBe(true);
    });

    it('sends a signed-in user to the home page', () => {
      seedSession(makeToken());

      expect(serialise(run(guestGuard))).toBe('/');
    });
  });

  describe('roleGuard', () => {
    it('lets an allowed role through', () => {
      seedSession(makeToken(makeUser({ role: AppRole.Admin })));

      expect(run(roleGuard(AppRole.Admin))).toBe(true);
    });

    it('turns other roles back to the home page', () => {
      seedSession(makeToken(makeUser({ role: AppRole.Developer })));

      expect(serialise(run(roleGuard(AppRole.Admin)))).toBe('/');
    });

    it('turns a signed-out visitor back to the home page', () => {
      expect(serialise(run(roleGuard(AppRole.Admin)))).toBe('/');
    });
  });
});
