import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import type { Observable } from 'rxjs';
import type {
  FileKind,
  ProjectDetail,
  ProjectFile,
  ProjectMember,
  TeamOverview,
  UserDto,
} from '../../../core/api/api-types';
import { problemMessage } from '../../../core/api/problem';
import { ProjectsApi } from '../../../core/api/projects-api';
import { AuthService } from '../../../core/auth/auth.service';
import { roleLabel } from '../../../core/auth/roles';
import { ToastService } from '../../../core/notifications/toast.service';
import { Icon } from '../../../shared/ui/icon/icon';
import { Stepper } from '../../../shared/ui/stepper/stepper';
import { AnalysisPanel } from '../analysis-panel/analysis-panel';
import { ProjectFiles, UploadRequest } from '../project-files/project-files';
import { ProjectTeam } from '../project-team/project-team';
import { StagePanel } from '../stage-panel/stage-panel';
import { TeamCapacity } from '../team-capacity/team-capacity';
import { STAGE_LABELS } from '../stage-labels';
import { WORKFLOW, blockReason, furthestStep, stepsFor } from '../workflow';

/** One project: where it is in the lifecycle, the stage being viewed, its input files and who is on it. */
@Component({
  selector: 'aisdlc-project-workspace',
  imports: [
    RouterLink,
    Icon,
    Stepper,
    AnalysisPanel,
    ProjectFiles,
    ProjectTeam,
    StagePanel,
    TeamCapacity,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-workspace.html',
  styleUrl: './project-workspace.scss',
})
export class ProjectWorkspacePage implements OnInit {
  private readonly api = inject(ProjectsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  /** Bound from the route parameter. */
  readonly projectId = input.required<string>();

  protected readonly project = signal<ProjectDetail | null>(null);
  protected readonly team = signal<TeamOverview | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly uploadingKind = signal<FileKind | null>(null);

  /** The stage being viewed. A project opens on its first stage, where the files are. */
  private readonly viewed = signal<number | null>(null);

  protected readonly stage = computed(() => this.project()?.stage ?? 'Draft');
  protected readonly steps = computed(() => stepsFor(this.stage()));
  protected readonly selectedIndex = computed(() => {
    return Math.min(this.viewed() ?? 0, furthestStep(this.stage()));
  });
  protected readonly selectedStep = computed(() => WORKFLOW[this.selectedIndex()]);
  protected readonly canGoPrevious = computed(() => this.selectedIndex() > 0);
  protected readonly nextBlockedReason = computed(() =>
    blockReason(this.stage(), this.selectedIndex()),
  );
  protected readonly canGoNext = computed(() => this.nextBlockedReason() === null);

  protected readonly stageLabels = STAGE_LABELS;
  protected readonly myRole = computed(() => roleLabel(this.auth.role() ?? ''));

  ngOnInit(): void {
    this.reload();
  }

  /** Gate 1 passed: reload so the stage, the stepper and the locked requirements follow. */
  protected analysisApproved(): void {
    this.reload();
  }

  protected goTo(index: number): void {
    if (index >= 0 && index <= furthestStep(this.stage())) {
      this.viewed.set(index);
    }
  }

  protected previous(): void {
    if (this.canGoPrevious()) {
      this.viewed.set(this.selectedIndex() - 1);
    }
  }

  protected next(): void {
    if (this.canGoNext()) {
      this.viewed.set(this.selectedIndex() + 1);
    }
  }

  protected upload(request: UploadRequest): void {
    this.uploadingKind.set(request.kind);
    this.act(
      this.api.upload(this.projectId(), request.kind, request.file),
      `${request.file.name} uploaded.`,
      () => this.uploadingKind.set(null),
    );
  }

  protected download(file: ProjectFile): void {
    this.api
      .download(this.projectId(), file.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          this.saveToDisk(blob, file.fileName);
          this.toast.success(`Downloading ${file.fileName}.`);
        },
        error: (error: unknown) => this.toast.error(problemMessage(error)),
      });
  }

  protected removeFile(file: ProjectFile): void {
    this.act(
      this.api.deleteFile(this.projectId(), file.id),
      `${file.fileName} (v${file.version}) deleted.`,
    );
  }

  protected changeCadence(weeks: number): void {
    this.act(
      this.api.setSprintCadence(this.projectId(), weeks),
      `Sprint length set to ${weeks} weeks.`,
    );
  }

  protected addMember(user: UserDto): void {
    this.act(
      this.api.addMember(this.projectId(), user.id),
      `${user.displayName} added to the project.`,
    );
  }

  protected removeMember(member: ProjectMember): void {
    this.act(
      this.api.removeMember(this.projectId(), member.userId),
      `${member.displayName} removed from the project.`,
    );
  }

  /** Runs a change, tells the user how it went, then reloads so the screen shows what the server holds. */
  private act(call: Observable<unknown>, success: string, done?: () => void): void {
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(success);
        done?.();
        this.reload();
      },
      error: (error: unknown) => {
        this.toast.error(problemMessage(error));
        done?.();
      },
    });
  }

  private reload(): void {
    this.api
      .getTeam(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (team) => this.team.set(team),
        error: (error: unknown) => this.toast.error(problemMessage(error)),
      });

    this.api
      .get(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (project) => {
          this.project.set(project);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.notFound.set(true);
          this.toast.error(problemMessage(error));
          this.loading.set(false);
        },
      });
  }

  private saveToDisk(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
