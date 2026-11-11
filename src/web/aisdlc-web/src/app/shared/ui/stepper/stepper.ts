import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '../icon/icon';

export type StepState = 'done' | 'current' | 'upcoming';

export interface Step {
  label: string;
  caption: string;
  state: StepState;
}

/** The lifecycle at a glance: each stage shows done, current or not started. */
@Component({
  selector: 'aisdlc-stepper',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol aria-label="Project stages">
      @for (step of steps(); track step.label; let last = $last) {
        <li [class]="step.state" [attr.aria-current]="step.state === 'current' ? 'step' : null">
          <span class="dot">
            @if (step.state === 'done') {
              <aisdlc-icon name="check" [size]="16" />
            } @else {
              <span class="sr-only">{{
                step.state === 'current' ? 'Current:' : 'Not started:'
              }}</span>
            }
          </span>
          <span class="label">{{ step.label }}</span>
          <span class="caption">{{ step.caption }}</span>
          @if (!last) {
            <span class="line" aria-hidden="true"></span>
          }
        </li>
      }
    </ol>
  `,
  styleUrl: './stepper.scss',
})
export class Stepper {
  readonly steps = input.required<readonly Step[]>();
}
