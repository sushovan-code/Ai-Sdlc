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
import { UsersApi } from '../../core/api/users-api';
import type { UserDto } from '../../core/api/api-types';
import { problemMessage } from '../../core/api/problem';
import { AuthService } from '../../core/auth/auth.service';
import { ALL_ROLES, roleLabel } from '../../core/auth/roles';
import { ToastService } from '../../core/notifications/toast.service';

const PAGE_SIZE = 20;

/** Administrator screen: change a user's role or deactivate an account. */
@Component({
  selector: 'aisdlc-users',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class UsersPage implements OnInit {
  private readonly api = inject(UsersApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastService);
  private readonly currentUserId = inject(AuthService).user()?.id;

  protected readonly roles = ALL_ROLES;
  protected readonly roleLabel = roleLabel;
  protected readonly users = signal<UserDto[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)),
  );

  ngOnInit(): void {
    this.load(1);
  }

  protected isSelf(user: UserDto): boolean {
    return user.id === this.currentUserId;
  }

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.load(page);
    }
  }

  protected changeRole(user: UserDto, role: string): void {
    this.run(this.api.setRole(user.id, role), `${user.displayName} is now ${roleLabel(role)}.`);
  }

  protected toggleActive(user: UserDto): void {
    const next = !user.isActive;
    this.run(
      this.api.setActive(user.id, next),
      `${user.displayName} was ${next ? 'reactivated' : 'deactivated'}.`,
    );
  }

  private run(call: ReturnType<UsersApi['setRole']>, success: string): void {
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.users.update((list) => list.map((u) => (u.id === updated.id ? updated : u)));
        this.toast.success(success);
      },
      error: (error: unknown) => {
        this.toast.error(problemMessage(error));
        this.load(this.page());
      },
    });
  }

  private load(page: number): void {
    this.loading.set(true);
    this.api
      .list(page, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.page.set(result.page);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.toast.error(problemMessage(error));
          this.loading.set(false);
        },
      });
  }
}
