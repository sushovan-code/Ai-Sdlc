import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { AppRole } from '../../../core/auth/roles';
import { Brand } from '../../ui/brand/brand';
import { Icon } from '../../ui/icon/icon';
import { RolePill } from '../../ui/role-pill/role-pill';
import { ThemeSwitcher } from '../../ui/theme-switcher/theme-switcher';

const INITIALS_LIMIT = 2;

/** Top bar and content frame for every signed-in page. Navigation items depend on the caller's role. */
@Component({
  selector: 'aisdlc-app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Brand, Icon, RolePill, ThemeSwitcher],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.user;
  protected readonly isAdmin = computed(() => this.auth.hasAnyRole([AppRole.Admin]));
  protected readonly initials = computed(() =>
    (this.user()?.displayName ?? '')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, INITIALS_LIMIT)
      .map((word) => word[0].toUpperCase())
      .join(''),
  );

  protected signOut(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
