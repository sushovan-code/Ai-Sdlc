import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PROJECT_ID, makeFile, makeProject, onTheWire } from '../../testing/project-fixtures';
import { ProjectsApi } from './projects-api';

describe('ProjectsApi', () => {
  let api: ProjectsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ProjectsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('turns the string numbers of the list response into real numbers', () => {
    let totalCount = 0;
    let memberCount = 0;

    api.list().subscribe((page) => {
      totalCount = page.totalCount;
      memberCount = page.items[0].memberCount;
    });
    http
      .expectOne((r) => r.url === '/api/v1/projects')
      .flush({
        items: [
          {
            id: 'a',
            name: 'A',
            description: null,
            stage: 'Draft',
            createdAtUtc: 'x',
            memberCount: '3',
            hasRequirements: false,
            hasTeamTemplate: false,
          },
        ],
        page: '1',
        pageSize: '24',
        totalCount: '1',
      });

    expect(totalCount).toBe(1);
    expect(memberCount).toBe(3);
  });

  it('normalises version and size on a project detail', () => {
    let version: unknown;
    let size: unknown;

    api.get(PROJECT_ID).subscribe((project) => {
      version = project.files[0].version;
      size = project.files[0].sizeBytes;
    });
    http
      .expectOne(`/api/v1/projects/${PROJECT_ID}`)
      .flush({ ...makeProject(), files: [onTheWire(makeFile({ version: 2, sizeBytes: 4096 }))] });

    expect(version).toBe(2);
    expect(size).toBe(4096);
  });

  it('uploads as multipart to the route for the kind', () => {
    const file = new File(['abc'], 'team.xlsx');

    api.upload(PROJECT_ID, 'TeamTemplate', file).subscribe();

    const request = http.expectOne(`/api/v1/projects/${PROJECT_ID}/files/team-template`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect((request.request.body as FormData).get('file')).toBeInstanceOf(File);
    request.flush(onTheWire(makeFile({ kind: 'TeamTemplate' })));
  });

  it('downloads as a blob', () => {
    let received: Blob | undefined;

    api.download(PROJECT_ID, 'f1').subscribe((blob) => (received = blob));
    const request = http.expectOne(`/api/v1/projects/${PROJECT_ID}/files/f1/download`);
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['x']));

    expect(received).toBeInstanceOf(Blob);
  });

  it('sends member changes and deletes to the right URLs', () => {
    api.addMember(PROJECT_ID, 'u1').subscribe();
    const add = http.expectOne(`/api/v1/projects/${PROJECT_ID}/members`);
    expect(add.request.body).toEqual({ userId: 'u1' });
    add.flush({});

    api.removeMember(PROJECT_ID, 'u1').subscribe();
    http
      .expectOne({ method: 'DELETE', url: `/api/v1/projects/${PROJECT_ID}/members/u1` })
      .flush(null);

    api.deleteFile(PROJECT_ID, 'f1').subscribe();
    http
      .expectOne({ method: 'DELETE', url: `/api/v1/projects/${PROJECT_ID}/files/f1` })
      .flush(null);
  });
});
