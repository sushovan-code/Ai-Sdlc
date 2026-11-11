import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  const root = document.documentElement;

  beforeEach(() => {
    localStorage.clear();
    root.removeAttribute('data-theme');
  });

  it('defaults to System and leaves the data-theme attribute off', () => {
    const theme = TestBed.inject(ThemeService);

    expect(theme.mode()).toBe('system');
    expect(root.hasAttribute('data-theme')).toBe(false);
  });

  it.each(['light', 'dark'] as const)('applies and saves %s', (mode) => {
    const theme = TestBed.inject(ThemeService);

    theme.set(mode);

    expect(theme.mode()).toBe(mode);
    expect(root.getAttribute('data-theme')).toBe(mode);
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe(mode);
  });

  it('going back to System removes the attribute and the saved choice', () => {
    const theme = TestBed.inject(ThemeService);
    theme.set('dark');

    theme.set('system');

    expect(root.hasAttribute('data-theme')).toBe(false);
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
  });

  it('restores the saved choice on start', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');

    const theme = TestBed.inject(ThemeService);

    expect(theme.mode()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('ignores a corrupt saved value', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'neon');

    expect(TestBed.inject(ThemeService).mode()).toBe('system');
  });
});
