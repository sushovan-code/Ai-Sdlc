import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import type { ProjectSummary } from '../../core/api/api-types';
import { fieldErrors, problemMessage } from '../../core/api/problem';
import { ProjectsApi } from '../../core/api/projects-api';
import { AuthService } from '../../core/auth/auth.service';
import { AppRole } from '../../core/auth/roles';
import { Icon } from '../../shared/ui/icon/icon';
import { STAGE_LABELS, stagePillClass } from './stage-labels';

const MIN_NAME_LENGTH = 3;
const MAX_NAME_LENGTH = 100;
const MAX_DESCRIPTION_LENGTH = 500;

/** The projects the signed-in user belongs to, plus a create form for Project Managers. */
@Component({
  selector: 'aisdlc-projects',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './projects.html',
  styleUrl: './projects.scss',
})
export class ProjectsPage implements OnInit {
  private readonly api = inject(ProjectsApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly auth = inject(AuthService);

  protected readonly canCreate = computed(() => this.auth.hasAnyRole([AppRole.ProjectManager]));
  protected readonly projects = signal<ProjectSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly creating = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly nameError = signal<string | null>(null);
  protected readonly stageLabels = STAGE_LABELS;
  protected readonly stagePillClass = stagePillClass;

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(MIN_NAME_LENGTH),
        Validators.maxLength(MAX_NAME_LENGTH),
      ],
    ],
    description: ['', [Validators.maxLength(MAX_DESCRIPTION_LENGTH)]],
  });

  ngOnInit(): void {
    this.api
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.projects.set(page.items);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(problemMessage(error));
          this.loading.set(false);
        },
      });
  }

  protected create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.creating.set(true);
    this.errorMessage.set(null);
    this.nameError.set(null);
    const { name, description } = this.form.getRawValue();
    this.api
      .create({ name: name.trim(), description: description.trim() || null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (project) => void this.router.navigate(['/projects', project.id]),
        error: (error: unknown) => {
          this.nameError.set(fieldErrors(error)['name']?.[0] ?? null);
          this.errorMessage.set(problemMessage(error));
          this.creating.set(false);
        },
      });
  }

  protected nameInvalid(): boolean {
    const control = this.form.controls.name;
    return control.invalid && control.touched;
  }
}
