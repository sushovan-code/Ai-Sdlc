import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

interface ProblemBody {
  title?: string;
  errors?: Record<string, string[]>;
}

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.';
const RATE_LIMIT_MESSAGE = 'Too many attempts. Please wait a minute and try again.';
const OFFLINE_MESSAGE = 'Cannot reach the server. Check that the API is running.';

function bodyOf(error: unknown): ProblemBody {
  if (error instanceof HttpErrorResponse && typeof error.error === 'object' && error.error) {
    return error.error as ProblemBody;
  }
  return {};
}

/** A message safe to show a user, taken from the RFC 9457 Problem Details title when there is one. */
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return OFFLINE_MESSAGE;
    }
    if (error.status === HttpStatusCode.TooManyRequests) {
      return RATE_LIMIT_MESSAGE;
    }
  }
  return bodyOf(error).title ?? FALLBACK_MESSAGE;
}

/** Field name (camelCase) to messages, from a validation problem. */
export function fieldErrors(error: unknown): Record<string, string[]> {
  const raw = bodyOf(error).errors ?? {};
  return Object.fromEntries(
    Object.entries(raw).map(([field, messages]) => [
      field.charAt(0).toLowerCase() + field.slice(1),
      messages,
    ]),
  );
}
