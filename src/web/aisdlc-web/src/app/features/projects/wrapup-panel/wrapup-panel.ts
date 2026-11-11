import { DOCUMENT, DatePipe } from '@angular/common';
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
import { problemMessage } from '../../../core/api/problem';
import { WrapupApi } from '../../../core/api/wrapup-api';
import type { AuditEntry, Traceability } from '../../../core/api/wrapup-types';
import { AuthService } from '../../../core/auth/auth.service';
import { AppRole } from '../../../core/auth/roles';
import { ToastService } from '../../../core/notifications/toast.service';
import { Icon } from '../../../shared/ui/icon/icon';

const AUDIT_PAGE_SIZE = 25;

/** The last step: trace each requirement through the stages, read the audit trail and take the report. */
@Component({
  selector: 'aisdlc-wrapup-panel',
  imports: [DatePipe, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './wrapup-panel.html',
  styleUrl: './wrapup-panel.scss',
})
export class WrapupPanel implements OnInit {
  private readonly api = inject(WrapupApi);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<string>();

  protected readonly trace = signal<Traceability | null>(null);
  protected readonly audit = signal<AuditEntry[]>([]);
  protected readonly auditTotal = signal(0);
  protected readonly loadingAudit = signal(false);
  protected readonly downloading = signal(false);
  private page = 0;

  /** The audit trail names people and files, so only the roles that review the work can read it. */
  protected readonly canSeeAudit = computed(() =>
    this.auth.hasAnyRole([AppRole.ProjectManager, AppRole.Architect, AppRole.QA]),
  );

  protected readonly hasMoreAudit = computed(() => this.audit().length < this.auditTotal());
  protected readonly gapCount = computed(
    () =>
      this.trace()?.rows.filter(
        (r) => r.architecture.length === 0 || r.tasks.length === 0 || r.testCases.length === 0,
      ).length ?? 0,
  );

  ngOnInit(): void {
    this.api
      .traceability(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (trace) => this.trace.set(trace),
        error: (error: unknown) => this.toast.error(problemMessage(error)),
      });
    if (this.canSeeAudit()) {
      this.loadMoreAudit();
    }
  }

  protected loadMoreAudit(): void {
    this.loadingAudit.set(true);
    this.api
      .audit(this.projectId(), this.page + 1, AUDIT_PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.page = result.page;
          this.audit.update((rows) => [...rows, ...result.items]);
          this.auditTotal.set(result.totalCount);
          this.loadingAudit.set(false);
        },
        error: (error: unknown) => {
          this.loadingAudit.set(false);
          this.toast.error(problemMessage(error));
        },
      });
  }

  protected downloadReport(): void {
    this.downloading.set(true);
    this.api
      .report(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          this.saveToDisk(blob, 'project-report.md');
          this.downloading.set(false);
          this.toast.success('Downloading the report.');
          this.reloadAudit();
        },
        error: (error: unknown) => {
          this.downloading.set(false);
          this.toast.error(problemMessage(error));
        },
      });
  }

  /** The download is itself audited, so show it. */
  private reloadAudit(): void {
    if (this.canSeeAudit()) {
      this.page = 0;
      this.audit.set([]);
      this.loadMoreAudit();
    }
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
