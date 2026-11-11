import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const FULL = 100;
const WARNING_AT = 85;

/** A horizontal bar showing how much of a total is used. Turns amber when nearly full and red when over. */
@Component({
  selector: 'aisdlc-progress-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="track"
      role="progressbar"
      [attr.aria-label]="label()"
      aria-valuemin="0"
      [attr.aria-valuemax]="max()"
      [attr.aria-valuenow]="value()"
      [attr.aria-valuetext]="valueText()"
    >
      <div class="fill" [class]="'fill ' + tone()" [style.width.%]="width()"></div>
    </div>
  `,
  styles: `
    .track {
      height: 10px;
      overflow: hidden;
      border-radius: 999px;
      background: var(--border);
    }

    .fill {
      height: 100%;
      border-radius: 999px;
      background: var(--accent);
      transition: width var(--motion);
    }

    .fill.warning {
      background: var(--warning);
    }

    .fill.over {
      background: var(--danger);
    }
  `,
})
export class ProgressBar {
  readonly value = input.required<number>();
  readonly max = input.required<number>();
  readonly label = input.required<string>();
  /** Spoken description, for example "12 of 60 hours". */
  readonly valueText = input<string | null>(null);

  protected readonly percent = computed(() =>
    this.max() <= 0 ? 0 : (this.value() / this.max()) * FULL,
  );
  protected readonly width = computed(() => Math.min(FULL, Math.max(0, this.percent())));
  protected readonly tone = computed(() => {
    if (this.percent() > FULL) {
      return 'over';
    }
    return this.percent() >= WARNING_AT ? 'warning' : 'normal';
  });
}
