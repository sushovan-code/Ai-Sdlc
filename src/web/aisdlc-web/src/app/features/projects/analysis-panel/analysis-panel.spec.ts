import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { AnalysisOverview } from '../../../core/api/analysis-types';
import { makeOverview, makeVersion } from '../../../testing/analysis-fixtures';
import { click, host, textOf, typeInto } from '../../../testing/dom';
import { PROJECT_ID } from '../../../testing/project-fixtures';
import { toastMessages } from '../../../testing/toasts';
import { AnalysisPanel } from './analysis-panel';

describe('AnalysisPanel', () => {
  let fixture: ComponentFixture<AnalysisPanel>;
  let http: HttpTestingController;
  let approvedCount: number;
  const url = `/api/v1/projects/${PROJECT_ID}/analysis`;

  async function render(overview: AnalysisOverview, canManage = true): Promise<void> {
    approvedCount = 0;
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AnalysisPanel);
    fixture.componentRef.setInput('projectId', PROJECT_ID);
    fixture.componentRef.setInput('canManage', canManage);
    fixture.componentInstance.approved.subscribe(() => approvedCount++);
    fixture.detectChanges();
    http.expectOne(url).flush(overview);
    await fixture.whenStable();
  }

  const text = () => textOf(host(fixture));
  const button = (label: string) =>
    Array.from(host(fixture).querySelectorAll('button')).find((b) =>
      (b.textContent ?? '').includes(label),
    );

  afterEach(() => http.verify());

  it('offers to generate when there is no analysis yet and the user is a manager', async () => {
    await render(makeOverview());

    expect(text()).toContain('No analysis yet');
    expect(button('Generate with AI')).toBeTruthy();
  });

  it('says why the analysis cannot start yet', async () => {
    await render(makeOverview({ canGenerate: false, blockedReason: 'Upload the files first.' }));

    expect(text()).toContain('Upload the files first.');
    expect(button('Generate with AI')).toBeUndefined();
  });

  it('shows a non-manager the draft read-only', async () => {
    await render(makeOverview({ versions: [makeVersion()] }), false);

    expect(text()).toContain('Pay with a saved card');
    expect(text()).toContain('Only the Project Manager can generate and review');
    expect(button('Approve')).toBeUndefined();
    expect(button('Regenerate')).toBeUndefined();
  });

  it('shows the draft with its requirements, questions and evidence', async () => {
    await render(makeOverview({ versions: [makeVersion()] }));

    expect(text()).toContain('Version 1 · Waiting for review');
    expect(text()).toContain('FR-1');
    expect(text()).toContain('Which cards are accepted?');
    expect(text()).toContain('Evidence from the document (1)');
  });

  it('generates a first draft and reloads', async () => {
    await render(makeOverview());

    button('Generate with AI')!.click();
    http.expectOne(`${url}/generate`).flush(makeVersion());
    http.expectOne(url).flush(makeOverview({ versions: [makeVersion()] }));
    await fixture.whenStable();

    expect(toastMessages('success')).toContain('Draft ready for review.');
    expect(text()).toContain('Version 1');
  });

  it('regenerates with the typed feedback', async () => {
    await render(makeOverview({ versions: [makeVersion()] }));

    typeInto(host(fixture), '#analysis-comment', 'Focus on refunds');
    button('Regenerate')!.click();

    const request = http.expectOne(`${url}/generate`);
    expect(request.request.body).toEqual({ feedback: 'Focus on refunds' });
    request.flush(makeVersion({ version: 2 }));
    http.expectOne(url).flush(makeOverview({ versions: [makeVersion({ version: 2 })] }));
  });

  it('needs a reason before it can reject', async () => {
    await render(makeOverview({ versions: [makeVersion()] }));
    expect(button('Reject')!.disabled).toBe(true);

    typeInto(host(fixture), '#analysis-comment', 'Too thin');
    fixture.detectChanges();
    expect(button('Reject')!.disabled).toBe(false);
    button('Reject')!.click();

    const request = http.expectOne(`${url}/versions/1/reject`);
    expect(request.request.body).toEqual({ comment: 'Too thin' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne(url).flush(makeOverview({ versions: [makeVersion({ status: 'Rejected' })] }));
  });

  it('approves the newest version and tells the page', async () => {
    await render(makeOverview({ versions: [makeVersion({ version: 2 })] }));

    click(host(fixture), 'button.btn-primary');
    const request = http.expectOne(`${url}/versions/2/approve`);
    request.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne(url).flush(
      makeOverview({
        canGenerate: false,
        blockedReason: 'The analysis has already been approved.',
        versions: [
          makeVersion({
            version: 2,
            status: 'Approved',
            decisions: [
              {
                kind: 'Approved',
                reviewerId: 'u1',
                reviewerRole: 'ProjectManager',
                comment: null,
                decidedAtUtc: '2026-11-11T11:00:00Z',
              },
            ],
          }),
        ],
      }),
    );
    await fixture.whenStable();

    expect(approvedCount).toBe(1);
    expect(text()).toContain('requirements are now locked');
    expect(button('Approve')).toBeUndefined();
  });

  it('shows the server message when generating fails', async () => {
    await render(makeOverview());

    button('Generate with AI')!.click();
    http
      .expectOne(`${url}/generate`)
      .flush({ title: 'The AI model could not be reached.' }, { status: 503, statusText: 'x' });
    await fixture.whenStable();

    expect(toastMessages('error')).toContain('The AI model could not be reached.');
    expect(button('Generate with AI')!.disabled).toBe(false);
  });
});
