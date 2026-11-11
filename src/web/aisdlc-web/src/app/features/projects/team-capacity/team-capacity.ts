import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SPRINT_WEEK_OPTIONS, type TeamOverview } from '../../../core/api/api-types';
import { ProgressBar } from '../../../shared/ui/progress-bar/progress-bar';

/** The people on the team sheet, what each can give in a sprint and how much of it is booked. */
@Component({
  selector: 'aisdlc-team-capacity',
  imports: [DecimalPipe, ProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './team-capacity.html',
  styleUrl: './team-capacity.scss',
})
export class TeamCapacity {
  readonly team = input<TeamOverview | null>(null);
  readonly canManage = input(false);
  readonly cadenceChange = output<number>();

  protected readonly weekOptions = SPRINT_WEEK_OPTIONS;

  protected chooseWeeks(event: Event): void {
    const weeks = Number((event.target as HTMLSelectElement).value);
    if (weeks !== this.team()?.sprintWeeks) {
      this.cadenceChange.emit(weeks);
    }
  }
}
