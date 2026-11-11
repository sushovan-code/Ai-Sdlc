import { TestBed } from '@angular/core/testing';
import { ToastService, type ToastKind } from '../core/notifications/toast.service';

/** The messages currently on screen, newest first. */
export function toastMessages(kind?: ToastKind): string[] {
  return TestBed.inject(ToastService)
    .toasts()
    .filter((toast) => !kind || toast.kind === kind)
    .map((toast) => toast.message);
}
