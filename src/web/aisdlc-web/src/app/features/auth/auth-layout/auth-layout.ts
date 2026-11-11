import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Brand } from '../../../shared/ui/brand/brand';
import { Icon, IconName } from '../../../shared/ui/icon/icon';
import { ThemeSwitcher } from '../../../shared/ui/theme-switcher/theme-switcher';

interface Highlight {
  icon: IconName;
  title: string;
  text: string;
}

const HIGHLIGHTS: readonly Highlight[] = [
  {
    icon: 'shield-check',
    title: 'Four human approval gates',
    text: 'Nothing moves forward until a named reviewer signs off.',
  },
  {
    icon: 'eye',
    title: 'AI-assisted, never autonomous',
    text: 'Drafts come with evidence, critic notes and guardrail flags.',
  },
  {
    icon: 'history',
    title: 'Complete audit trail',
    text: 'Every upload, decision and edit is recorded and exportable.',
  },
];

/** Split-screen frame shared by the sign-in and register pages: brand story left, form right. */
@Component({
  selector: 'aisdlc-auth-layout',
  imports: [Brand, Icon, ThemeSwitcher],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="hero" aria-label="About AI-Assisted SDLC">
      <aisdlc-brand />
      <div class="pitch">
        <h1>Ship software faster, with people in control.</h1>
        <p class="lead">
          From requirements to architecture, tasks and test cases. AI drafts it, the right person
          approves it.
        </p>
        <ul>
          @for (item of highlights; track item.title) {
            <li>
              <span class="badge"><aisdlc-icon [name]="item.icon" [size]="20" /></span>
              <span>
                <strong>{{ item.title }}</strong>
                <span class="sub">{{ item.text }}</span>
              </span>
            </li>
          }
        </ul>
      </div>
    </section>
    <main class="panel">
      <div class="theme"><aisdlc-theme-switcher /></div>
      <div class="content"><ng-content /></div>
    </main>
  `,
  styleUrl: './auth-layout.scss',
})
export class AuthLayout {
  protected readonly highlights = HIGHLIGHTS;
}
