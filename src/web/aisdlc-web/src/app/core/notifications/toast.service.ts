import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'info' | 'warning' | 'error';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  /** How long it stays, or null when it stays until dismissed (errors). */
  autoDismissMs: number | null;
}

export const MAX_VISIBLE_TOASTS = 5;

const AUTO_DISMISS_MS: Record<ToastKind, number | null> = {
  success: 4000,
  info: 4000,
  warning: 7000,
  error: null,
};

interface Countdown {
  handle: ReturnType<typeof setTimeout>;
  startedAt: number;
  remainingMs: number;
}

/**
 * Short messages about what just happened. Newest on top, at most five at once (the rest wait their turn),
 * errors stay until dismissed, identical messages are not stacked, and the timer pauses while the user is reading.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly active = signal<Toast[]>([]);
  private readonly waiting: Toast[] = [];
  private readonly countdowns = new Map<number, Countdown>();
  private nextId = 1;

  readonly toasts = this.active.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  warning(message: string): void {
    this.show('warning', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  dismiss(id: number): void {
    this.stopCountdown(id);
    this.active.update((list) => list.filter((toast) => toast.id !== id));
    const next = this.waiting.shift();
    if (next) {
      this.activate(next);
    }
  }

  /** Holds the countdown while the pointer or keyboard focus is on a toast. */
  pause(id: number): void {
    const countdown = this.countdowns.get(id);
    if (!countdown) {
      return;
    }
    clearTimeout(countdown.handle);
    countdown.remainingMs -= Date.now() - countdown.startedAt;
  }

  resume(id: number): void {
    const countdown = this.countdowns.get(id);
    if (countdown) {
      this.startCountdown(id, countdown.remainingMs);
    }
  }

  private show(kind: ToastKind, message: string): void {
    const duplicate = [...this.active(), ...this.waiting].some(
      (toast) => toast.kind === kind && toast.message === message,
    );
    if (duplicate) {
      return;
    }

    const toast: Toast = { id: this.nextId++, kind, message, autoDismissMs: AUTO_DISMISS_MS[kind] };
    if (this.active().length < MAX_VISIBLE_TOASTS) {
      this.activate(toast);
    } else {
      this.waiting.push(toast);
    }
  }

  private activate(toast: Toast): void {
    this.active.update((list) => [toast, ...list]);
    if (toast.autoDismissMs !== null) {
      this.startCountdown(toast.id, toast.autoDismissMs);
    }
  }

  private startCountdown(id: number, ms: number): void {
    const handle = setTimeout(() => this.dismiss(id), ms);
    this.countdowns.set(id, { handle, startedAt: Date.now(), remainingMs: ms });
  }

  private stopCountdown(id: number): void {
    const countdown = this.countdowns.get(id);
    if (countdown) {
      clearTimeout(countdown.handle);
      this.countdowns.delete(id);
    }
  }
}
