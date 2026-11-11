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
  UserDto,
} from '../../../core/api/api-types';
import { problemMessage } from '../../../core/api/problem';
import { ProjectsApi } from '../../../core/api/projects-api';
import { AuthService } from '../../../core/auth/auth.service';
import { roleLabel } from '../../../core/auth/roles';
import { Icon } from '../../../shared/ui/icon/icon';
import { Stepper } from '../../../shared/ui/stepper/stepper';
import { ProjectFiles, UploadRequest } from '../project-files/project-files';
import { ProjectTeam } from '../project-team/project-team';
import { STAGE_LABELS, lifecycleSteps, stagePillClass } from '../stage-labels';

/** One project: where it is in the lifecycle, its input files and who is on it. */
@Component({
  selector: 'aisdlc-project-workspace',
  imports: [RouterLink, Icon, Stepper, ProjectFiles, ProjectTeam],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-workspace.html',
  styleUrl: './project-workspace.scss',
})
export class ProjectWorkspacePage implements OnInit {
  private readonly api = inject(ProjectsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly auth = inject(AuthService);

  /** Bound from the route parameter. */
  readonly projectId = input.required<string>();

  protected readonly project = signal<ProjectDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly statusMessage = signal<string | null>(null);
  protected readonly uploadingKind = signal<FileKind | null>(null);

  protected readonly steps = computed(() => lifecycleSteps(this.project()?.stage ?? 'Draft'));
  protected readonly stageLabels = STAGE_LABELS;
  protected readonly stagePillClass = stagePillClass;
  protected readonly myRole = computed(() => roleLabel(this.auth.role() ?? ''));

  ngOnInit(): void {
    this.reload();
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
    this.errorMessage.set(null);
    this.api
      .download(this.projectId(), file.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => this.saveToDisk(blob, file.fileName),
        error: (error: unknown) => this.errorMessage.set(problemMessage(error)),
      });
  }

  protected removeFile(file: ProjectFile): void {
    this.act(
      this.api.deleteFile(this.projectId(), file.id),
      `${file.fileName} (v${file.version}) deleted.`,
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

  /** Runs a change, then reloads the project so the screen always shows what the server holds. */
  private act(call: Observable<unknown>, success: string, done?: () => void): void {
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.statusMessage.set(success);
        done?.();
        this.reload();
      },
      error: (error: unknown) => {
        this.errorMessage.set(problemMessage(error));
        done?.();
      },
    });
  }

  private reload(): void {
    this.api
      .get(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (project) => {
          this.project.set(project);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(problemMessage(error));
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
