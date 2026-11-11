import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ThemeMode, ThemeService } from '../../../core/theme/theme.service';
import { Icon, IconName } from '../icon/icon';

interface ThemeOption {
  mode: ThemeMode;
  label: string;
  icon: IconName;
}

const THEME_OPTIONS: readonly ThemeOption[] = [
  { mode: 'light', label: 'Light', icon: 'sun' },
  { mode: 'dark', label: 'Dark', icon: 'moon' },
  { mode: 'system', label: 'System', icon: 'monitor' },
];

/** Segmented control for Light, Dark and System. */
@Component({
  selector: 'aisdlc-theme-switcher',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="group" role="group" aria-label="Colour theme">
      @for (option of options; track option.mode) {
        <button
          type="button"
          [attr.aria-pressed]="theme.mode() === option.mode"
          [attr.aria-label]="option.label"
          [title]="option.label"
          (click)="theme.set(option.mode)"
        >
          <aisdlc-icon [name]="option.icon" [size]="16" />
        </button>
      }
    </div>
  `,
  styles: `
    .group {
      display: inline-flex;
      padding: 2px;
      border: 1px solid var(--border);
      border-radius: 999px;
      background: var(--surface-2);
    }

    button {
      display: grid;
      place-items: center;
      width: 30px;
      height: 30px;
      border: 0;
      border-radius: 999px;
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;
      transition: background var(--motion);
    }

    button[aria-pressed='true'] {
      background: var(--accent-soft);
      color: var(--accent);
    }
  `,
})
export class ThemeSwitcher {
  protected readonly theme = inject(ThemeService);
  protected readonly options = THEME_OPTIONS;
}
