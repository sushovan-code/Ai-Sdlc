import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';
import { ToastHost } from './shared/ui/toast-host/toast-host';

@Component({
  selector: 'aisdlc-root',
  imports: [RouterOutlet, ToastHost],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <router-outlet />
    <aisdlc-toast-host />
  `,
})
export class App {
  // Injecting the service applies the saved theme as soon as the app starts.
  protected readonly theme = inject(ThemeService);
}
