import { Injectable } from '@angular/core';
import type { AuthTokenDto } from '../api/api-types';

const SESSION_KEY = 'aisdlc.session';

/**
 * Keeps the access token in sessionStorage: it is cleared when the tab closes and is not shared between tabs.
 * Storage can be unavailable (private mode, blocked site data), so every access is guarded.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  read(): AuthTokenDto | null {
    try {
      const raw = sessionStorage.getItem(SESSION_KEY);
      return raw ? (JSON.parse(raw) as AuthTokenDto) : null;
    } catch {
      return null;
    }
  }

  write(session: AuthTokenDto): void {
    try {
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    } catch {
      /* the session then lives in memory only */
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      /* nothing to clear */
    }
  }
}
