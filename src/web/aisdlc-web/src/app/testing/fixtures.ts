import type { AuthTokenDto, UserDto } from '../core/api/api-types';
import { AppRole } from '../core/auth/roles';

const ONE_HOUR_MS = 60 * 60 * 1000;
const SESSION_KEY = 'aisdlc.session';

export function makeUser(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'pm@demo.local',
    displayName: 'Demo Project Manager',
    role: AppRole.ProjectManager,
    isActive: true,
    ...overrides,
  };
}

export function makeToken(user: UserDto = makeUser(), expiresInMs = ONE_HOUR_MS): AuthTokenDto {
  return {
    accessToken: 'test-token',
    expiresAtUtc: new Date(Date.now() + expiresInMs).toISOString(),
    user,
  };
}

/** Puts a session where the app's SessionStore reads it, before the AuthService is created. */
export function seedSession(token: AuthTokenDto): void {
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(token));
}
