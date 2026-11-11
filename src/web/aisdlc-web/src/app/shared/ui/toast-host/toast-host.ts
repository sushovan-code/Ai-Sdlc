import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastKind, ToastService } from '../../../core/notifications/toast.service';
import { Icon, IconName } from '../icon/icon';

const ICONS: Record<ToastKind, IconName> = {
  success: 'check-circle',
  info: 'info',
  warning: 'alert',
  error: 'alert',
};

/** Shows the active toasts in the corner of the screen. Placed once, in the root component. */
@Component({
  selector: 'aisdlc-toast-host',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="host" aria-label="Notifications">
      @for (toast of toasts(); track toast.id) {
        <div
          class="toast"
          [class]="'toast ' + toast.kind"
          [attr.role]="toast.kind === 'error' ? 'alert' : 'status'"
          (mouseenter)="service.pause(toast.id)"
          (mouseleave)="service.resume(toast.id)"
          (focusin)="service.pause(toast.id)"
          (focusout)="service.resume(toast.id)"
        >
          <aisdlc-icon [name]="icon(toast.kind)" [size]="20" />
          <span class="message">{{ toast.message }}</span>
          <button
            type="button"
            class="close"
            aria-label="Dismiss notification"
            (click)="service.dismiss(toast.id)"
          >
            <aisdlc-icon name="x" [size]="16" />
          </button>
          @if (toast.autoDismissMs; as duration) {
            <span class="bar" aria-hidden="true" [style.animation-duration.ms]="duration"></span>
          }
        </div>
      }
    </section>
  `,
  styleUrl: './toast-host.scss',
})
export class ToastHost {
  protected readonly service = inject(ToastService);
  protected readonly toasts = this.service.toasts;

  protected icon(kind: ToastKind): IconName {
    return ICONS[kind];
  }
}
