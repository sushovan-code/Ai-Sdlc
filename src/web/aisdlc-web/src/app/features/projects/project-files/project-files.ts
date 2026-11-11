import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { formatBytes } from '../../../core/api/file-limits';
import type { FileKind, ProjectDetail, ProjectFile } from '../../../core/api/api-types';
import { Icon } from '../../../shared/ui/icon/icon';
import { FileDrop } from '../file-drop/file-drop';

export interface UploadRequest {
  kind: FileKind;
  file: File;
}

const REQUIREMENT_EXTENSIONS = ['.docx', '.txt'] as const;
const TEAM_EXTENSIONS = ['.xlsx'] as const;
const SHORT_HASH_LENGTH = 8;

/** Upload targets and the version history of a project's two input files. */
@Component({
  selector: 'aisdlc-project-files',
  imports: [DatePipe, FileDrop, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-files.html',
  styleUrl: './project-files.scss',
})
export class ProjectFiles {
  readonly project = input.required<ProjectDetail>();
  readonly busyKind = input<FileKind | null>(null);
  readonly upload = output<UploadRequest>();
  readonly download = output<ProjectFile>();
  readonly remove = output<ProjectFile>();

  protected readonly requirementExtensions = REQUIREMENT_EXTENSIONS;
  protected readonly teamExtensions = TEAM_EXTENSIONS;
  protected readonly formatBytes = formatBytes;

  /** The file whose delete is waiting for a second click. */
  protected readonly pendingDelete = signal<string | null>(null);

  protected readonly requirements = computed(() => this.filesOf('Requirements'));
  protected readonly team = computed(() => this.filesOf('TeamTemplate'));

  protected shortHash(file: ProjectFile): string {
    return file.sha256.slice(0, SHORT_HASH_LENGTH);
  }

  protected confirmRemove(file: ProjectFile): void {
    this.pendingDelete.set(null);
    this.remove.emit(file);
  }

  private filesOf(kind: FileKind): ProjectFile[] {
    return this.project().files.filter((f) => f.kind === kind);
  }
}
