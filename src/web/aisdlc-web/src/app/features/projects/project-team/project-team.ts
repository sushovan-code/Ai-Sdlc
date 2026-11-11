import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import type { ProjectDetail, ProjectMember, UserDto } from '../../../core/api/api-types';
import { ProjectsApi } from '../../../core/api/projects-api';
import { Icon } from '../../../shared/ui/icon/icon';
import { RolePill } from '../../../shared/ui/role-pill/role-pill';

const SEARCH_DEBOUNCE_MS = 250;

/** Who can see this project. A Project Manager can find colleagues and add or remove them. */
@Component({
  selector: 'aisdlc-project-team',
  imports: [Icon, RolePill],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-team.html',
  styleUrl: './project-team.scss',
})
export class ProjectTeam {
  private readonly api = inject(ProjectsApi);

  readonly project = input.required<ProjectDetail>();
  readonly add = output<UserDto>();
  readonly remove = output<ProjectMember>();

  protected readonly query = signal('');
  protected readonly results = signal<UserDto[]>([]);
  protected readonly searching = signal(false);

  /** Search hits that are not already on the project. */
  protected readonly candidates = computed(() => {
    const present = new Set(this.project().members.map((m) => m.userId));
    return this.results().filter((user) => !present.has(user.id));
  });

  /** True once the first search has come back, so "nobody to add" is not shown while loading. */
  protected readonly loaded = signal(false);

  constructor() {
    toObservable(this.query)
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((text) => {
          this.searching.set(true);
          return this.api.searchPeople(text.trim()).pipe(catchError(() => of<UserDto[]>([])));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((users) => {
        this.results.set(users);
        this.searching.set(false);
        this.loaded.set(true);
      });
  }

  protected pick(user: UserDto): void {
    this.add.emit(user);
    this.query.set('');
  }
}
