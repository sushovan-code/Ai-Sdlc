import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
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
import { problemMessage } from '../../../core/api/problem';
import { StageApi } from '../../../core/api/stage-api';
import type { StageContent, StageKey, StageOverview } from '../../../core/api/stage-types';
import { AuthService } from '../../../core/auth/auth.service';
import { roleLabel } from '../../../core/auth/roles';
import { ToastService } from '../../../core/notifications/toast.service';
import { Icon } from '../../../shared/ui/icon/icon';

type Action = 'generate' | 'approve' | 'reject' | 'save';

const STATUS_LABELS: Record<string, string> = {
  PendingReview: 'Waiting for review',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Superseded: 'Replaced by a newer version',
};

/**
 * Gates 2 to 4 on one simple page: generate the draft, read it, correct it by hand if needed, then approve, reject or
 * ask for another draft. The same page serves architecture, tasks and test cases; only the heading text changes.
 */
@Component({
  selector: 'aisdlc-stage-review',
  imports: [DatePipe, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stage-review.html',
  styleUrl: './stage-review.scss',
})
export class StageReview implements OnInit {
  private readonly api = inject(StageApi);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<string>();
  readonly stage = input.required<StageKey>();
  readonly title = input.required<string>();
  readonly intro = input.required<string>();

  /** Fired after the gate is approved, so the page can move the project on. */
  readonly approved = output<void>();

  protected readonly overview = signal<StageOverview | null>(null);
  protected readonly busy = signal<Action | null>(null);
  protected readonly comment = signal('');
  protected readonly loadFailed = signal(false);
  /** The copy being corrected, or null when not editing. */
  protected readonly draft = signal<StageContent | null>(null);
  private readonly approvedBanner = viewChild<ElementRef<HTMLElement>>('approvedBanner');

  protected readonly latest = computed(() => this.overview()?.versions[0] ?? null);
  protected readonly isPending = computed(() => this.latest()?.status === 'PendingReview');
  protected readonly isApproved = computed(() => this.latest()?.status === 'Approved');
  /** Only the person whose role reviews this stage can act on it. */
  protected readonly canAct = computed(
    () => !!this.overview() && this.auth.role() === this.overview()?.approverRole,
  );
  protected readonly canRegenerate = computed(
    () => this.canAct() && !!this.overview()?.canGenerate && !this.isPending(),
  );
  protected readonly canEdit = computed(
    () => this.canAct() && !!this.overview()?.canGenerate && !!this.latest(),
  );
  protected readonly olderVersions = computed(() => (this.overview()?.versions ?? []).slice(1));
  protected readonly approval = computed(() =>
    this.latest()?.decisions.find((d) => d.kind === 'Approved'),
  );
  protected readonly roleLabel = roleLabel;

  constructor() {
    // After approving, the pressed button disappears; move focus to the confirmation instead of losing it.
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
      this.api.generate(this.projectId(), this.stage(), this.trimmedComment()),
      'Draft ready for review.',
    );
  }

  protected approve(): void {
    const latest = this.latest();
    if (latest) {
      this.run(
        'approve',
        this.api.approve(this.projectId(), this.stage(), latest.version, this.trimmedComment()),
        'Approved. The next stage is now unlocked.',
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
        this.api.reject(this.projectId(), this.stage(), latest.version, reason),
        'Draft rejected. Ask for another draft when ready.',
      );
    }
  }

  protected startEditing(): void {
    const latest = this.latest();
    if (latest) {
      this.draft.set(structuredClone(latest.content));
    }
  }

  protected cancelEditing(): void {
    this.draft.set(null);
  }

  protected saveEdit(): void {
    const content = this.draft();
    if (content) {
      this.run(
        'save',
        this.api.edit(this.projectId(), this.stage(), content),
        'Your changes are saved as a new version.',
        () => this.draft.set(null),
      );
    }
  }

  protected setSummary(event: Event): void {
    this.change((content) => (content.summary = this.valueOf(event)));
  }

  protected setTitle(section: number, item: number, event: Event): void {
    this.change((content) => (content.sections[section].items[item].title = this.valueOf(event)));
  }

  protected setText(section: number, item: number, event: Event): void {
    this.change((content) => (content.sections[section].items[item].text = this.valueOf(event)));
  }

  protected setTags(section: number, item: number, event: Event): void {
    this.change(
      (content) =>
        (content.sections[section].items[item].tags = this.valueOf(event)
          .split(',')
          .map((tag) => tag.trim())
          .filter((tag) => tag.length > 0)),
    );
  }

  private valueOf(event: Event): string {
    return (event.target as HTMLInputElement | HTMLTextAreaElement).value;
  }

  private change(edit: (content: StageContent) => void): void {
    const content = this.draft();
    if (content) {
      edit(content);
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
        after?.();
        this.load();
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
      .get(this.projectId(), this.stage())
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
