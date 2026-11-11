import { ComponentFixture, TestBed } from '@angular/core/testing';
import { host } from '../../../testing/dom';
import { ProgressBar } from './progress-bar';

describe('ProgressBar', () => {
  let fixture: ComponentFixture<ProgressBar>;

  async function render(
    value: number,
    max: number,
    valueText: string | null = null,
  ): Promise<void> {
    fixture = TestBed.createComponent(ProgressBar);
    fixture.componentRef.setInput('value', value);
    fixture.componentRef.setInput('max', max);
    fixture.componentRef.setInput('label', 'Utilization of Asha Rao');
    fixture.componentRef.setInput('valueText', valueText);
    await fixture.whenStable();
  }

  const bar = () => host(fixture).querySelector('[role="progressbar"]') as HTMLElement;
  const fill = () => host(fixture).querySelector('.fill') as HTMLElement;

  it('exposes its value to assistive technology', async () => {
    await render(12, 60, '12 of 60 hours booked');

    expect(bar().getAttribute('aria-label')).toBe('Utilization of Asha Rao');
    expect(bar().getAttribute('aria-valuenow')).toBe('12');
    expect(bar().getAttribute('aria-valuemax')).toBe('60');
    expect(bar().getAttribute('aria-valuetext')).toBe('12 of 60 hours booked');
  });

  it('fills in proportion to the value', async () => {
    await render(15, 60);

    expect(fill().style.width).toBe('25%');
    expect(fill().className).toContain('normal');
  });

  it('turns amber when nearly full', async () => {
    await render(51, 60);

    expect(fill().className).toContain('warning');
  });

  it('turns red and stops at the end of the track when over capacity', async () => {
    await render(70, 60);

    expect(fill().className).toContain('over');
    expect(fill().style.width).toBe('100%');
  });

  it('shows an empty bar when there is no capacity, without dividing by zero', async () => {
    await render(0, 0);

    expect(fill().style.width).toBe('0%');
  });
});
