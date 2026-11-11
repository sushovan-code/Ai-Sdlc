import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AppRole } from '../../core/auth/roles';
import { click, host, textOf, typeInto } from '../../testing/dom';
import { makeToken, makeUser, seedSession } from '../../testing/fixtures';
import { ProjectsPage } from './projects';

const SUMMARY = {
  id: 'p1',
  name: 'Express Checkout',
  description: 'Faster pay',
  stage: 'FilesUploaded',
  createdAtUtc: '2026-11-11T09:00:00Z',
  memberCount: '3',
  hasRequirements: true,
  hasTeamTemplate: true,
};

describe('ProjectsPage', () => {
  let fixture: ComponentFixture<ProjectsPage>;
  let http: HttpTestingController;
  let router: Router;

  async function render(role: string, projects: unknown[] = [SUMMARY]): Promise<void> {
    sessionStorage.clear();
    seedSession(makeToken(makeUser({ role })));
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ProjectsPage);
    await fixture.whenStable();
    http
      .expectOne((r) => r.url === '/api/v1/projects')
      .flush({ items: projects, page: '1', pageSize: '24', totalCount: String(projects.length) });
    await fixture.whenStable();
  }

  afterEach(() => http.verify());

  it('shows each project as a card with its stage, files and team size', async () => {
    await render(AppRole.Developer);

    const text = textOf(host(fixture));
    expect(text).toContain('Express Checkout');
    expect(text).toContain('Files uploaded');
    expect(text).toContain('Requirements ✓');
    expect(text).toContain('3 member(s)');
    expect(host(fixture).querySelector('a[href="/projects/p1"]')).not.toBeNull();
  });

  it('hides the create form from everyone but Project Managers', async () => {
    await render(AppRole.Developer);

    expect(host(fixture).querySelector('#project-name')).toBeNull();
  });

  it('tells a member without projects that a manager will add them', async () => {
    await render(AppRole.QA, []);

    expect(textOf(host(fixture))).toContain('A Project Manager will add you to a project.');
  });

  it('lets a Project Manager create a project and opens it', async () => {
    await render(AppRole.ProjectManager, []);
    typeInto(host(fixture), '#project-name', '  Express Checkout ');
    typeInto(host(fixture), '#project-description', 'Faster pay');

    click(host(fixture), 'button[type="submit"]');
    const request = http.expectOne({ method: 'POST', url: '/api/v1/projects' });
    expect(request.request.body).toEqual({ name: 'Express Checkout', description: 'Faster pay' });
    request.flush({ ...SUMMARY, id: 'new-id' });
    await fixture.whenStable();

    expect(router.navigate).toHaveBeenCalledWith(['/projects', 'new-id']);
  });

  it('does not call the API for a name that is too short', async () => {
    await render(AppRole.ProjectManager, []);
    typeInto(host(fixture), '#project-name', 'ab');

    click(host(fixture), 'button[type="submit"]');
    await fixture.whenStable();

    http.expectNone({ method: 'POST', url: '/api/v1/projects' });
    expect(textOf(host(fixture))).toContain('Enter a name of 3 to 100 characters.');
  });
});
