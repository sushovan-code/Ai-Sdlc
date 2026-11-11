import { toastMessages } from '../../../testing/toasts';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppRole } from '../../../core/auth/roles';
import { click, host, textOf } from '../../../testing/dom';
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

describe('ProjectWorkspacePage', () => {
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
    respondWith(project);
    await fixture.whenStable();
  }

  function respondWith(project: ReturnType<typeof makeProject>): void {
    http.expectOne({ method: 'GET', url: PROJECT_URL }).flush({
      ...project,
      files: project.files.map(onTheWire),
    });
  }

  afterEach(() => http.verify());

  it('shows the project, who you are and the lifecycle', async () => {
    await render();

    const text = textOf(host(fixture));
    expect(text).toContain('Express Checkout');
    expect(text).toContain('You are Project Manager');
    expect(text).toContain('Draft');
    expect(host(fixture).querySelectorAll('aisdlc-stepper li').length).toBe(5);
  });

  it('moves the stepper past "Files & team" once both inputs are in', async () => {
    await render(
      makeProject({
        stage: 'FilesUploaded',
        files: [makeFile(), makeFile({ id: 'f2', kind: 'TeamTemplate', fileName: 'team.xlsx' })],
      }),
    );

    const steps = Array.from(host(fixture).querySelectorAll('aisdlc-stepper li'));
    expect(steps[0].className).toContain('done');
    expect(steps[1].className).toContain('current');
  });

  it('lists every version, marking the latest', async () => {
    await render(
      makeProject({
        files: [
          makeFile({ id: 'f2', version: 2, isLatest: true }),
          makeFile({ id: 'f1', version: 1, isLatest: false }),
        ],
      }),
    );

    const rows = host(fixture).querySelectorAll('.file');
    expect(textOf(rows[0] as HTMLElement)).toContain('v2');
    expect(textOf(rows[0] as HTMLElement)).toContain('Latest');
    expect(textOf(rows[1] as HTMLElement)).not.toContain('Latest');
  });

  it('uploads a picked file, then reloads what the server holds', async () => {
    await render();
    const input = host(fixture).querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['hello'], 'reqs.txt');
    Object.defineProperty(input, 'files', {
      value: { 0: file, length: 1, item: () => file },
      configurable: true,
    });

    input.dispatchEvent(new Event('change', { bubbles: true }));
    http
      .expectOne({ method: 'POST', url: `${PROJECT_URL}/files/requirements` })
      .flush(onTheWire(makeFile({ fileName: 'reqs.txt' })));
    respondWith(makeProject({ files: [makeFile({ fileName: 'reqs.txt' })] }));
    await fixture.whenStable();

    expect(toastMessages()).toContain('reqs.txt uploaded.');
    expect(host(fixture).querySelectorAll('.file').length).toBe(1);
  });

  it('shows the server message when an upload is rejected', async () => {
    await render();
    const input = host(fixture).querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['not a zip'], 'fake.docx');
    Object.defineProperty(input, 'files', {
      value: { 0: file, length: 1, item: () => file },
      configurable: true,
    });

    input.dispatchEvent(new Event('change', { bubbles: true }));
    http
      .expectOne({ method: 'POST', url: `${PROJECT_URL}/files/requirements` })
      .flush(
        { title: 'One or more validation errors occurred.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(toastMessages().join(' ')).toContain('validation errors');
  });

  it('asks for a second click before deleting a file', async () => {
    await render(makeProject({ files: [makeFile()] }));

    click(host(fixture), '.file .btn-danger');
    await fixture.whenStable();
    http.expectNone({ method: 'DELETE', url: `${PROJECT_URL}/files/${makeFile().id}` });
    expect(textOf(host(fixture))).toContain('Confirm delete');

    click(host(fixture), '.file .btn-danger');
    http.expectOne({ method: 'DELETE', url: `${PROJECT_URL}/files/${makeFile().id}` }).flush(null);
    respondWith(makeProject());
    await fixture.whenStable();

    expect(toastMessages().join(' ')).toContain('deleted.');
  });

  it('offers upload, delete and member changes only to a manager', async () => {
    await render(
      makeProject({
        canManage: false,
        files: [makeFile()],
        members: [makeMember(), makeMember({ userId: 'u2', displayName: 'Ana', isOwner: false })],
      }),
      AppRole.Developer,
    );

    expect(host(fixture).querySelector('input[type="file"]')).toBeNull();
    expect(host(fixture).querySelector('.file .btn-danger')).toBeNull();
    expect(host(fixture).querySelector('#member-search')).toBeNull();
    expect(textOf(host(fixture))).toContain('Download');
    expect(textOf(host(fixture))).not.toContain('Remove');
  });

  it('adds a found colleague to the team', async () => {
    await render();
    const search = host(fixture).querySelector('#member-search') as HTMLInputElement;
    search.value = 'ana';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    await new Promise((resolve) => setTimeout(resolve, 300));
    http
      .expectOne((r) => r.url === '/api/v1/directory/users' && r.params.get('query') === 'ana')
      .flush([
        {
          id: 'u2',
          email: 'ana@demo.local',
          displayName: 'Ana Architect',
          role: 'Architect',
          isActive: true,
        },
      ]);
    await fixture.whenStable();

    click(host(fixture), 'button[aria-label="Add Ana Architect"]');
    http.expectOne({ method: 'POST', url: `${PROJECT_URL}/members` }).flush({});
    respondWith(makeProject());
    await fixture.whenStable();

    expect(toastMessages()).toContain('Ana Architect added to the project.');
  });

  it('shows "not found" when the project is missing or you are not on it', async () => {
    sessionStorage.clear();
    seedSession(makeToken());
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProjectWorkspacePage);
    fixture.componentRef.setInput('projectId', PROJECT_ID);
    await fixture.whenStable();

    http
      .expectOne({ method: 'GET', url: PROJECT_URL })
      .flush({ title: 'Project not found.' }, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(toastMessages().join(' ')).toContain('Project not found.');
  });
});
