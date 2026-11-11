import { TestBed } from '@angular/core/testing';
import { MAX_VISIBLE_TOASTS, ToastService } from './toast.service';

describe('ToastService', () => {
  let toast: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    toast = TestBed.inject(ToastService);
  });

  afterEach(() => vi.useRealTimers());

  const messages = () => toast.toasts().map((t) => t.message);

  it('shows the newest toast on top', () => {
    toast.success('first');
    toast.success('second');

    expect(messages()).toEqual(['second', 'first']);
  });

  it('removes a success toast by itself after a few seconds', () => {
    toast.success('saved');

    vi.advanceTimersByTime(3999);
    expect(messages()).toEqual(['saved']);
    vi.advanceTimersByTime(2);
    expect(messages()).toEqual([]);
  });

  it('keeps a warning a little longer than a success', () => {
    toast.warning('careful');

    vi.advanceTimersByTime(5000);
    expect(messages()).toEqual(['careful']);
    vi.advanceTimersByTime(2500);
    expect(messages()).toEqual([]);
  });

  it('keeps an error until the user dismisses it', () => {
    toast.error('it failed');

    vi.advanceTimersByTime(10 * 60 * 1000);
    expect(messages()).toEqual(['it failed']);

    toast.dismiss(toast.toasts()[0].id);
    expect(messages()).toEqual([]);
  });

  it('does not stack the same message twice', () => {
    toast.error('same');
    toast.error('same');
    toast.success('same');

    expect(toast.toasts().map((t) => t.kind)).toEqual(['success', 'error']);
  });

  it('shows at most five at once and lets the rest wait their turn', () => {
    for (let i = 1; i <= MAX_VISIBLE_TOASTS + 2; i++) {
      toast.error(`error ${i}`);
    }
    expect(toast.toasts().length).toBe(MAX_VISIBLE_TOASTS);
    expect(messages()).not.toContain('error 6');

    toast.dismiss(toast.toasts()[0].id);

    expect(toast.toasts().length).toBe(MAX_VISIBLE_TOASTS);
    expect(messages()[0]).toBe('error 6');
  });

  it('holds the countdown while the user is reading and continues afterwards', () => {
    toast.success('read me');
    const { id } = toast.toasts()[0];

    vi.advanceTimersByTime(3000);
    toast.pause(id);
    vi.advanceTimersByTime(60_000);
    expect(messages()).toEqual(['read me']);

    toast.resume(id);
    vi.advanceTimersByTime(1100);
    expect(messages()).toEqual([]);
  });

  it('allows the same message again once the first has gone', () => {
    toast.info('hello');
    vi.advanceTimersByTime(5000);

    toast.info('hello');

    expect(messages()).toEqual(['hello']);
  });
});
