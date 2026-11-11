import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../icon/icon';

@Component({
  selector: 'aisdlc-brand',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="mark"><aisdlc-icon name="sparkles" [size]="20" /></span>
    <span class="name">AI-Assisted <strong>SDLC</strong></span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 12px;
      color: inherit;
      font-weight: 600;
    }

    .mark {
      display: grid;
      place-items: center;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: linear-gradient(135deg, #4f46e5, #8b5cf6);
      color: #fff;
    }

    strong {
      color: var(--brand-accent, var(--accent));
    }
  `,
})
export class Brand {}
