import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Icon } from '../icon/icon';

export type StepState = 'done' | 'current' | 'upcoming';

export interface Step {
  label: string;
  caption: string;
  state: StepState;
  /** Whether the user may open this stage now. */
  reachable: boolean;
}

/** The lifecycle at a glance. Stages the user can reach are buttons; locked ones are not. */
@Component({
  selector: 'aisdlc-stepper',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol aria-label="Project stages">
      @for (step of steps(); track step.label; let index = $index; let last = $last) {
        <li [class]="step.state" [class.selected]="index === selectedIndex()">
          <button
            type="button"
            class="step"
            [disabled]="!step.reachable"
            [attr.aria-current]="index === selectedIndex() ? 'step' : null"
            (click)="stepChosen.emit(index)"
          >
            <span class="dot">
              @if (step.state === 'done') {
                <aisdlc-icon name="check" [size]="16" />
              } @else if (!step.reachable) {
                <aisdlc-icon name="lock" [size]="14" />
              } @else {
                <span class="number">{{ index + 1 }}</span>
              }
            </span>
            <span class="label">{{ step.label }}</span>
            <span class="caption">{{ step.caption }}</span>
          </button>
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
  readonly selectedIndex = input(0);
  readonly stepChosen = output<number>();
}
