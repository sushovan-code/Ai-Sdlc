import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAX_FILE_BYTES } from '../../../core/api/file-limits';
import { host, textOf } from '../../../testing/dom';
import { FileDrop } from './file-drop';

function choose(fixture: ComponentFixture<FileDrop>, file: File): void {
  const input = host(fixture).querySelector('input[type="file"]') as HTMLInputElement;
  Object.defineProperty(input, 'files', {
    value: { 0: file, length: 1, item: () => file },
    configurable: true,
  });
  input.dispatchEvent(new Event('change', { bubbles: true }));
}

function sized(name: string, bytes: number): File {
  return new File([new Uint8Array(bytes)], name);
}

describe('FileDrop', () => {
  let fixture: ComponentFixture<FileDrop>;
  const picked: File[] = [];

  beforeEach(async () => {
    picked.length = 0;
    fixture = TestBed.createComponent(FileDrop);
    fixture.componentRef.setInput('title', 'Requirements document');
    fixture.componentRef.setInput('hint', '.docx or .txt, up to 10 MB');
    fixture.componentRef.setInput('accept', ['.docx', '.txt']);
    fixture.componentInstance.picked.subscribe((file) => picked.push(file));
    await fixture.whenStable();
  });

  it('passes an acceptable file on', async () => {
    choose(fixture, sized('reqs.DOCX', 10));
    await fixture.whenStable();

    expect(picked.map((f) => f.name)).toEqual(['reqs.DOCX']);
    expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
  });

  it('refuses the wrong file type and says which types are allowed', async () => {
    choose(fixture, sized('malware.exe', 10));
    await fixture.whenStable();

    expect(picked).toEqual([]);
    expect(textOf(host(fixture))).toContain('Only .docx, .txt files are accepted here.');
  });

  it('refuses a file over 10 MB', async () => {
    choose(fixture, sized('big.txt', MAX_FILE_BYTES + 1));
    await fixture.whenStable();

    expect(picked).toEqual([]);
    expect(textOf(host(fixture))).toContain('The limit is 10 MB.');
  });

  it('refuses an empty file', async () => {
    choose(fixture, sized('empty.txt', 0));
    await fixture.whenStable();

    expect(picked).toEqual([]);
    expect(textOf(host(fixture))).toContain('That file is empty.');
  });

  it('offers a new version once something is uploaded', async () => {
    fixture.componentRef.setInput('uploaded', true);
    await fixture.whenStable();

    expect(textOf(host(fixture))).toContain('Upload a new version');
    expect(textOf(host(fixture))).toContain('Uploaded');
  });

  it('shows who may upload instead of a picker when disabled', async () => {
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();

    expect(host(fixture).querySelector('input[type="file"]')).toBeNull();
    expect(textOf(host(fixture))).toContain('Only a Project Manager can upload.');
  });
});
