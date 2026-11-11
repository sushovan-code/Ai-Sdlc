import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, switchMap, tap } from 'rxjs';
import {
  API_BASE,
  type AuthTokenDto,
  type ChangePasswordRequest,
  type RegisterRequest,
  type UserDto,
} from '../api/api-types';
import type { AppRole } from './roles';
import { SessionStore } from './session-store';

/** Sign-in state and the auth API calls. Components read the signals; nothing else holds the token. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(SessionStore);

  private readonly session = signal<AuthTokenDto | null>(this.restore());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly role = computed(() => this.user()?.role ?? null);

  /** The bearer token, or null when signed out or expired. */
  accessToken(): string | null {
    const current = this.session();
    if (!current) {
      return null;
    }
    if (this.isExpired(current)) {
      this.logout();
      return null;
    }
    return current.accessToken;
  }

  login(email: string, password: string): Observable<UserDto> {
    return this.http.post<AuthTokenDto>(`${API_BASE}/auth/login`, { email, password }).pipe(
      tap((token) => this.start(token)),
      map((token) => token.user),
    );
  }

  /** Creates the account, then signs in with the same credentials. */
  register(request: RegisterRequest): Observable<UserDto> {
    return this.http
      .post<UserDto>(`${API_BASE}/auth/register`, request)
      .pipe(switchMap(() => this.login(request.email, request.password)));
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE}/auth/change-password`, request);
  }

  logout(): void {
    this.store.clear();
    this.session.set(null);
  }

  hasAnyRole(roles: readonly AppRole[]): boolean {
    const current = this.role();
    return current !== null && (roles as readonly string[]).includes(current);
  }

  private start(token: AuthTokenDto): void {
    this.store.write(token);
    this.session.set(token);
  }

  private restore(): AuthTokenDto | null {
    const stored = this.store.read();
    if (!stored || this.isExpired(stored)) {
      this.store.clear();
      return null;
    }
    return stored;
  }

  private isExpired(token: AuthTokenDto): boolean {
    return new Date(token.expiresAtUtc).getTime() <= Date.now();
  }
}
