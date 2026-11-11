import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_BASE } from '../api/api-types';
import { ToastService } from '../notifications/toast.service';
import { AuthService } from './auth.service';

const LOGIN_URL = `${API_BASE}/auth/login`;

/** Adds the bearer token to API calls and sends the user to sign-in when the server says the token is no longer valid. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(API_BASE)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);
  const token = auth.accessToken();
  const authorised = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorised).pipe(
    catchError((error: unknown) => {
      const rejected =
        error instanceof HttpErrorResponse &&
        error.status === HttpStatusCode.Unauthorized &&
        request.url !== LOGIN_URL &&
        token !== null;
      if (rejected) {
        auth.logout();
        toast.warning('Your session has expired. Please sign in again.');
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};
