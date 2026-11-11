import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { roleLabel } from '../../../core/auth/roles';
import { Icon } from '../../../shared/ui/icon/icon';
import type { WorkflowStep } from '../workflow';

/** What a stage is for and which review gate closes it. The stage tools arrive in later releases. */
@Component({
  selector: 'aisdlc-stage-panel',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="icon"><aisdlc-icon name="sparkles" [size]="24" /></span>
    <h2>{{ step().label }}</h2>
    <p class="summary">{{ step().summary }}</p>
    @if (step().gate; as gate) {
      <p class="gate">
        <aisdlc-icon name="shield-check" [size]="16" />
        Gate {{ gate.number }}: {{ gate.name }} by the {{ roleLabel(gate.approver) }}
      </p>
    }
    <p class="soon">This stage is not available yet. It arrives in an upcoming release.</p>
  `,
  styles: `
    :host {
      display: grid;
      justify-items: center;
      gap: 8px;
      padding: 40px 24px;
      text-align: center;
    }

    .icon {
      display: grid;
      place-items: center;
      width: 52px;
      height: 52px;
      border-radius: 16px;
      background: var(--accent-soft);
      color: var(--accent);
    }

    h2 {
      font-size: 20px;
    }

    .summary {
      max-width: 520px;
      color: var(--text-muted);
    }

    .gate {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 12px;
      border-radius: 999px;
      background: var(--info-bg);
      color: var(--info);
      font-size: 13px;
      font-weight: 600;
    }

    .soon {
      color: var(--text-muted);
      font-size: 13px;
    }
  `,
})
export class StagePanel {
  readonly step = input.required<WorkflowStep>();

  protected readonly roleLabel = roleLabel;
}
