import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { MAX_FILE_BYTES, formatBytes } from '../../../core/api/file-limits';
import { Icon, IconName } from '../../../shared/ui/icon/icon';

let nextId = 0;

/** A click-or-drop target for one kind of file. Obvious mistakes (wrong type, too large) are caught here; the server re-checks everything. */
@Component({
  selector: 'aisdlc-file-drop',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './file-drop.html',
  styleUrl: './file-drop.scss',
})
export class FileDrop {
  readonly title = input.required<string>();
  readonly hint = input.required<string>();
  readonly accept = input.required<readonly string[]>();
  readonly icon = input<IconName>('upload');
  readonly uploaded = input(false);
  readonly busy = input(false);
  readonly disabled = input(false);
  readonly picked = output<File>();

  protected readonly inputId = `file-drop-${nextId++}`;
  protected readonly dragging = signal(false);
  protected readonly problem = signal<string | null>(null);

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (!this.disabled()) {
      this.dragging.set(true);
    }
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    if (!this.disabled()) {
      this.choose(event.dataTransfer?.files ?? null);
    }
  }

  protected onChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.choose(input.files);
    input.value = '';
  }

  private choose(files: FileList | null): void {
    this.problem.set(null);
    const file = files?.item(0);
    if (!file) {
      return;
    }

    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!this.accept().includes(extension)) {
      this.problem.set(`Only ${this.accept().join(', ')} files are accepted here.`);
    } else if (file.size > MAX_FILE_BYTES) {
      this.problem.set(`That file is ${formatBytes(file.size)}. The limit is 10 MB.`);
    } else if (file.size === 0) {
      this.problem.set('That file is empty.');
    } else {
      this.picked.emit(file);
    }
  }
}
