import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { roleLabel } from '../../../core/auth/roles';

@Component({
  selector: 'aisdlc-role-pill',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="pill">{{ label() }}</span>`,
})
export class RolePill {
  readonly role = input.required<string>();

  protected readonly label = computed(() => roleLabel(this.role()));
}
