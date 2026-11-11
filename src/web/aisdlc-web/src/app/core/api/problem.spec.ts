import { HttpErrorResponse } from '@angular/common/http';
import { fieldErrors, problemMessage } from './problem';

describe('problem helpers', () => {
  it('uses the Problem Details title as the message', () => {
    const error = new HttpErrorResponse({
      status: 401,
      error: { title: 'Invalid email or password.' },
    });

    expect(problemMessage(error)).toBe('Invalid email or password.');
  });

  it('explains rate limiting', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 429 }))).toMatch(/too many attempts/i);
  });

  it('explains an unreachable server', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 0 }))).toMatch(
      /cannot reach the server/i,
    );
  });

  it('falls back to a generic message for anything else', () => {
    expect(problemMessage(new Error('boom'))).toMatch(/something went wrong/i);
    expect(problemMessage(new HttpErrorResponse({ status: 500, error: 'html page' }))).toMatch(
      /something went wrong/i,
    );
  });

  it('returns validation errors keyed by camelCase field name', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { Password: ['too short'], email: ['invalid'] } },
    });

    expect(fieldErrors(error)).toEqual({ password: ['too short'], email: ['invalid'] });
  });

  it('returns no field errors for other failures', () => {
    expect(fieldErrors(new Error('x'))).toEqual({});
  });
});
