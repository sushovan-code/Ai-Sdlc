import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppRole } from '../../../core/auth/roles';
import { host, textOf } from '../../../testing/dom';
import { makeToken, makeUser, seedSession } from '../../../testing/fixtures';
import {
  PROJECT_ID,
  makeFile,
  makeMember,
  makeProject,
  onTheWire,
} from '../../../testing/project-fixtures';
import { ProjectWorkspacePage } from './project-workspace';

const PROJECT_URL = `/api/v1/projects/${PROJECT_ID}`;

describe('ProjectWorkspacePage step navigation', () => {
  let fixture: ComponentFixture<ProjectWorkspacePage>;
  let http: HttpTestingController;

  async function render(
    project = makeProject(),
    role: string = AppRole.ProjectManager,
  ): Promise<void> {
    sessionStorage.clear();
    seedSession(makeToken(makeUser({ role })));
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProjectWorkspacePage);
    fixture.componentRef.setInput('projectId', PROJECT_ID);
    await fixture.whenStable();
    http.expectOne({ method: 'GET', url: PROJECT_URL }).flush({
      ...project,
      files: project.files.map(onTheWire),
    });
    await fixture.whenStable();
  }

  const filesUploaded = () =>
    makeProject({
      stage: 'FilesUploaded',
      files: [makeFile(), makeFile({ id: 'f2', kind: 'TeamTemplate', fileName: 'team.xlsx' })],
    });

  const button = (label: string): HTMLButtonElement =>
    Array.from(host(fixture).querySelectorAll<HTMLButtonElement>('.pager button')).find((b) =>
      b.textContent?.includes(label),
    ) as HTMLButtonElement;

  const stepButton = (label: string): HTMLButtonElement =>
    Array.from(
      host(fixture).querySelectorAll<HTMLButtonElement>('aisdlc-stepper button.step'),
    ).find((b) => b.textContent?.includes(label)) as HTMLButtonElement;

  afterEach(() => {
    // The team picker looks people up shortly after it appears; answer any lookup still pending.
    http
      .match((r) => r.url === '/api/v1/directory/users')
      .filter((r) => !r.cancelled)
      .forEach((r) => r.flush([]));
    http.verify();
  });

  it('blocks Next on a draft and says what is missing', async () => {
    await render();

    expect(button('Next').disabled).toBe(true);
    expect(button('Previous').disabled).toBe(true);
    expect(textOf(host(fixture))).toContain(
      'Upload both the requirements and the team template to continue.',
    );
    expect(stepButton('Analysis').disabled).toBe(true);
  });

  it('places Previous and Next after the stage content, at the bottom of the page', async () => {
    await render(filesUploaded());

    const content = host(fixture).querySelector('aisdlc-project-files') as HTMLElement;
    const pager = host(fixture).querySelector('nav.pager') as HTMLElement;
    const position = content.compareDocumentPosition(pager);

    expect(position & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(host(fixture).querySelector('aisdlc-stepper nav.pager')).toBeNull();
  });

  it('opens a project on its first stage, with the files in view', async () => {
    await render(filesUploaded());

    expect(textOf(host(fixture))).toContain('Project files');
    expect(button('Previous').disabled).toBe(true);
  });

  it('allows Next once both files are uploaded and shows the next stage', async () => {
    await render(filesUploaded());
    expect(button('Next').disabled).toBe(false);

    button('Next').click();
    await fixture.whenStable();

    const text = textOf(host(fixture));
    expect(text).toContain('requirements into functional and non-functional requirements');
    expect(text).toContain('Gate 1: Requirements analysis review by the Project Manager');
    expect(text).not.toContain('Project files');
  });

  it('stops Next at a gate and names the approval needed', async () => {
    await render(filesUploaded());
    button('Next').click();
    await fixture.whenStable();

    expect(button('Next').disabled).toBe(true);
    expect(textOf(host(fixture))).toContain(
      'Gate 1 (Requirements analysis review) must be approved by the Project Manager first.',
    );
  });

  it('goes back with Previous', async () => {
    await render(filesUploaded());
    button('Next').click();
    await fixture.whenStable();

    button('Previous').click();
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Project files');
    expect(button('Previous').disabled).toBe(true);
    expect(button('Next').disabled).toBe(false);
  });

  it('lets the user jump to a reachable stage from the stepper but not to a locked one', async () => {
    await render(filesUploaded());

    expect(stepButton('Architecture').disabled).toBe(true);
    expect(stepButton('Analysis').disabled).toBe(false);
    stepButton('Analysis').click();
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Gate 1');
    expect(host(fixture).querySelector('aisdlc-stepper li.selected')?.textContent).toContain(
      'Analysis',
    );
  });

  it('applies the same gating to every role, because approval is a server decision', async () => {
    await render(makeProject({ canManage: false, stage: 'Draft' }), AppRole.QA);

    expect(button('Next').disabled).toBe(true);
  });

  it('suggests people to add straight away, without typing, leaving out current members', async () => {
    await render(makeProject({ members: [makeMember()] }));

    await new Promise((resolve) => setTimeout(resolve, 300));
    http
      .expectOne((r) => r.url === '/api/v1/directory/users' && r.params.get('query') === '')
      .flush([
        {
          id: makeMember().userId,
          email: 'pm@demo.local',
          displayName: 'Demo Project Manager',
          role: 'ProjectManager',
          isActive: true,
        },
        {
          id: 'u2',
          email: 'ana@demo.local',
          displayName: 'Ana Architect',
          role: 'Architect',
          isActive: true,
        },
      ]);
    await fixture.whenStable();

    const suggestions = host(fixture).querySelector('ul[aria-label="Search results"]');
    expect(textOf(suggestions as HTMLElement)).toContain('Ana Architect');
    expect(textOf(suggestions as HTMLElement)).not.toContain('Demo Project Manager');
  });
});
