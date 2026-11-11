import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/ui/icon/icon';

/** Landing page after sign-in. The project list and creation form arrive with the next slice. */
@Component({
  selector: 'aisdlc-projects',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header>
      <h1>Projects</h1>
      <p class="sub">
        Each project moves from requirements to approved test cases through four review gates.
      </p>
    </header>
    <section class="card empty" aria-labelledby="empty-title">
      <span class="icon"><aisdlc-icon name="folder" [size]="28" /></span>
      <h2 id="empty-title">Welcome, {{ firstName() }}</h2>
      <p>No projects yet. Project creation, file upload and the review gates are coming next.</p>
    </section>
  `,
  styles: `
    header {
      margin-bottom: 24px;
    }

    h1 {
      font-size: 32px;
      letter-spacing: -0.02em;
    }

    .sub {
      color: var(--text-muted);
    }

    .empty {
      display: grid;
      justify-items: center;
      gap: 8px;
      padding: 56px 24px;
      text-align: center;
    }

    .icon {
      display: grid;
      place-items: center;
      width: 56px;
      height: 56px;
      border-radius: 16px;
      background: var(--accent-soft);
      color: var(--accent);
    }

    .empty p {
      max-width: 420px;
      color: var(--text-muted);
    }
  `,
})
export class ProjectsPage {
  private readonly auth = inject(AuthService);

  protected firstName(): string {
    return (this.auth.user()?.displayName ?? '').split(/\s+/)[0] || 'there';
  }
}
