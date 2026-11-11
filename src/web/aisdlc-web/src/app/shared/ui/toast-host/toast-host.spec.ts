import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastService } from '../../../core/notifications/toast.service';
import { click, host, textOf } from '../../../testing/dom';
import { ToastHost } from './toast-host';

describe('ToastHost', () => {
  let fixture: ComponentFixture<ToastHost>;
  let toast: ToastService;

  beforeEach(async () => {
    toast = TestBed.inject(ToastService);
    fixture = TestBed.createComponent(ToastHost);
    await fixture.whenStable();
  });

  it('is a labelled notifications region', () => {
    expect(host(fixture).querySelector('section[aria-label="Notifications"]')).not.toBeNull();
  });

  it('shows each toast with its message', async () => {
    toast.success('Project created.');
    toast.error('Upload failed.');
    await fixture.whenStable();

    const text = textOf(host(fixture));
    expect(text).toContain('Project created.');
    expect(text).toContain('Upload failed.');
  });

  it('announces errors assertively and everything else politely', async () => {
    toast.error('Broken');
    toast.info('FYI');
    await fixture.whenStable();

    const toasts = Array.from(host(fixture).querySelectorAll('.toast'));
    expect(toasts.find((t) => t.textContent?.includes('Broken'))?.getAttribute('role')).toBe(
      'alert',
    );
    expect(toasts.find((t) => t.textContent?.includes('FYI'))?.getAttribute('role')).toBe('status');
  });

  it('lets the user dismiss a toast', async () => {
    toast.error('Stays until closed');
    await fixture.whenStable();

    click(host(fixture), 'button[aria-label="Dismiss notification"]');
    await fixture.whenStable();

    expect(host(fixture).querySelectorAll('.toast').length).toBe(0);
  });

  it('shows a progress bar only for toasts that leave by themselves', async () => {
    toast.success('Auto');
    toast.error('Manual');
    await fixture.whenStable();

    const toasts = Array.from(host(fixture).querySelectorAll('.toast'));
    expect(
      toasts.find((t) => t.textContent?.includes('Auto'))?.querySelector('.bar'),
    ).not.toBeNull();
    expect(toasts.find((t) => t.textContent?.includes('Manual'))?.querySelector('.bar')).toBeNull();
  });
});
