import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';
import { AnalysisApi } from '../../../core/api/analysis-api';
import type { AnalysisOverview } from '../../../core/api/analysis-types';
import { problemMessage } from '../../../core/api/problem';
import { roleLabel } from '../../../core/auth/roles';
import { ToastService } from '../../../core/notifications/toast.service';
import { Icon } from '../../../shared/ui/icon/icon';

const STATUS_LABELS: Record<string, string> = {
  PendingReview: 'Waiting for review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Superseded: 'Replaced by a newer version',
};

type Action = 'generate' | 'approve' | 'reject';

/** Gate 1 on one simple page: generate the analysis, read it, then approve, reject or ask for another draft. */
@Component({
  selector: 'aisdlc-analysis-panel',
  imports: [DatePipe, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './analysis-panel.html',
  styleUrl: './analysis-panel.scss',
})
export class AnalysisPanel implements OnInit {
  private readonly api = inject(AnalysisApi);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<string>();
  /** Whether the signed-in user is a Project Manager on this project, the gate 1 reviewer. */
  readonly canManage = input.required<boolean>();

  /** Fired after the analysis is approved, so the page can move the project on. */
  readonly approved = output<void>();

  protected readonly overview = signal<AnalysisOverview | null>(null);
  protected readonly busy = signal<Action | null>(null);
  protected readonly comment = signal('');
  protected readonly loadFailed = signal(false);
  private readonly approvedBanner = viewChild<ElementRef<HTMLElement>>('approvedBanner');

  protected readonly latest = computed(() => this.overview()?.versions[0] ?? null);
  protected readonly isPending = computed(() => this.latest()?.status === 'PendingReview');
  protected readonly isApproved = computed(() => this.latest()?.status === 'Approved');
  protected readonly canRegenerate = computed(
    () => this.canManage() && !!this.overview()?.canGenerate && !this.isPending(),
  );
  protected readonly olderVersions = computed(() => (this.overview()?.versions ?? []).slice(1));
  protected readonly approval = computed(() =>
    this.latest()?.decisions.find((d) => d.kind === 'Approved'),
  );
  protected readonly roleLabel = roleLabel;

  constructor() {
    // After approving, the button the user pressed disappears; move focus to the confirmation instead of losing it.
    effect(() => this.approvedBanner()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.load();
  }

  protected statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  protected typeComment(event: Event): void {
    this.comment.set((event.target as HTMLTextAreaElement).value);
  }

  protected generate(): void {
    this.run(
      'generate',
      this.api.generate(this.projectId(), this.trimmedComment()),
      'Draft ready for review.',
    );
  }

  protected approve(): void {
    const latest = this.latest();
    if (latest) {
      this.run(
        'approve',
        this.api.approve(this.projectId(), latest.version, this.trimmedComment()),
        'Analysis approved. Architecture is now unlocked.',
        () => this.approved.emit(),
      );
    }
  }

  protected reject(): void {
    const latest = this.latest();
    const reason = this.trimmedComment();
    if (latest && reason) {
      this.run(
        'reject',
        this.api.reject(this.projectId(), latest.version, reason),
        'Draft rejected. Ask for another draft when ready.',
      );
    }
  }

  private trimmedComment(): string | null {
    const text = this.comment().trim();
    return text.length > 0 ? text : null;
  }

  private run(
    action: Action,
    call: Observable<unknown>,
    success: string,
    after?: () => void,
  ): void {
    this.busy.set(action);
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.comment.set('');
        this.busy.set(null);
        this.toast.success(success);
        this.load();
        after?.();
      },
      error: (error: unknown) => {
        this.busy.set(null);
        this.toast.error(problemMessage(error));
      },
    });
  }

  protected load(): void {
    this.loadFailed.set(false);
    this.api
      .get(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (overview) => this.overview.set(overview),
        error: (error: unknown) => {
          this.loadFailed.set(true);
          this.toast.error(problemMessage(error));
        },
      });
  }
}
