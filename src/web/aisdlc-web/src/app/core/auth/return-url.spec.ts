import { safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it.each(['/projects', '/users?page=2', '/'])('keeps the in-app path %s', (path) => {
    expect(safeReturnUrl(path)).toBe(path);
  });

  it.each([
    ['an absolute URL', 'https://evil.example/phish'],
    ['a protocol-relative URL', '//evil.example'],
    ['a backslash trick', '/\\evil.example'],
    ['an empty string', ''],
    ['a relative path', 'projects'],
  ])('falls back to the home page for %s', (_label, value) => {
    expect(safeReturnUrl(value)).toBe('/');
  });

  it('falls back to the home page when there is no value', () => {
    expect(safeReturnUrl(null)).toBe('/');
    expect(safeReturnUrl(undefined)).toBe('/');
  });
});
