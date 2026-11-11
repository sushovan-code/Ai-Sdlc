import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PROJECT_ID } from '../../testing/project-fixtures';
import { AnalysisApi } from './analysis-api';

describe('AnalysisApi', () => {
  let api: AnalysisApi;
  let http: HttpTestingController;
  const base = `/api/v1/projects/${PROJECT_ID}/analysis`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AnalysisApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the analysis of a project', () => {
    api.get(PROJECT_ID).subscribe();

    const request = http.expectOne(base);
    expect(request.request.method).toBe('GET');
    request.flush({ canGenerate: true, blockedReason: null, versions: [] });
  });

  it('generates a draft and sends the feedback with it', () => {
    api.generate(PROJECT_ID, 'Add refunds').subscribe();

    const request = http.expectOne(`${base}/generate`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ feedback: 'Add refunds' });
    request.flush({});
  });

  it('approves a version with an optional comment', () => {
    api.approve(PROJECT_ID, 2, null).subscribe();

    const request = http.expectOne(`${base}/versions/2/approve`);
    expect(request.request.body).toEqual({ comment: null });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('rejects a version with the reason', () => {
    api.reject(PROJECT_ID, 2, 'Too thin').subscribe();

    const request = http.expectOne(`${base}/versions/2/reject`);
    expect(request.request.body).toEqual({ comment: 'Too thin' });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });
});
