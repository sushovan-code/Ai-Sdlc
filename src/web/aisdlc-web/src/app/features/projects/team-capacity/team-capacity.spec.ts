import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { TeamOverview } from '../../../core/api/api-types';
import { host, textOf } from '../../../testing/dom';
import { NO_TEAM, makeTeam, makeTeamMember } from '../../../testing/project-fixtures';
import { TeamCapacity } from './team-capacity';

describe('TeamCapacity', () => {
  let fixture: ComponentFixture<TeamCapacity>;
  const changes: number[] = [];

  async function render(team: TeamOverview | null, canManage = true): Promise<void> {
    changes.length = 0;
    fixture = TestBed.createComponent(TeamCapacity);
    fixture.componentRef.setInput('team', team);
    fixture.componentRef.setInput('canManage', canManage);
    fixture.componentInstance.cadenceChange.subscribe((weeks) => changes.push(weeks));
    await fixture.whenStable();
  }

  const select = () => host(fixture).querySelector('#sprint-weeks') as HTMLSelectElement;

  const team = () =>
    makeTeam({
      members: [
        makeTeamMember({
          id: 'a',
          name: 'Asha Rao',
          allocatedHours: 15,
          totalCapacityHours: 30,
          utilizationPercent: 50,
        }),
        makeTeamMember({
          id: 'r',
          name: 'Ravi Kumar',
          role: 'Developer',
          skills: ['Angular'],
          availabilityHours: 40,
          sprintCapacityHours: 40,
          totalCapacityHours: 40,
        }),
      ],
    });

  it('shows a loading note until the team arrives', async () => {
    await render(null);

    expect(textOf(host(fixture))).toContain('Loading team…');
  });

  it('asks for the team template when nobody is on the team yet', async () => {
    await render(NO_TEAM);

    expect(textOf(host(fixture))).toContain('Upload the team template');
  });

  it('lists each person with role, skills and what they can give per sprint', async () => {
    await render(team());

    const text = textOf(host(fixture));
    expect(text).toContain('Asha Rao');
    expect(text).toContain('Developer');
    expect(text).toContain('Angular');
    expect(text).toContain('30 h available per 2 weeks');
    expect(text).toContain('30 h per 2-week sprint');
  });

  it('shows each persons utilization as a progress bar in hours', async () => {
    await render(team());

    const bars = Array.from(host(fixture).querySelectorAll('[role="progressbar"]'));
    expect(bars.length).toBe(2);
    expect(bars[0].getAttribute('aria-label')).toBe('Utilization of Asha Rao');
    expect(bars[0].getAttribute('aria-valuenow')).toBe('15');
    expect(bars[0].getAttribute('aria-valuemax')).toBe('30');
    expect(bars[0].getAttribute('aria-valuetext')).toBe('15 of 30 hours booked');
    expect(textOf(host(fixture))).toContain('15 of 30 h · 50%');
  });

  it('shows team totals and the per-sprint breakdown', async () => {
    await render(team());

    expect(textOf(host(fixture))).toContain('Team: 0 of 70 hours booked across 1 sprint(s).');
    expect(textOf(host(fixture))).toContain('Sprint 1: 0 of 30 h');
  });

  it('offers 2, 3 and 4 week sprints with 2 as the default, and explains the 8-hour day', async () => {
    await render(team());

    const options = Array.from(select().options).map((o) => o.textContent?.trim());
    expect(options).toEqual(['2 weeks (default)', '3 weeks', '4 weeks']);
    expect(select().value).toBe('2');
    expect(textOf(host(fixture))).toContain('A working day counts as 8 hours.');
  });

  it('shows how a longer sprint scales the sheet figures', async () => {
    await render(
      makeTeam({
        sprintWeeks: 4,
        members: [makeTeamMember({ sprintCapacityHours: 60, totalCapacityHours: 60 })],
      }),
    );

    const text = textOf(host(fixture));
    expect(select().value).toBe('4');
    expect(text).toContain('a 4-week sprint gives each person 2× that');
    expect(text).toContain('60 h per 4-week sprint');
  });

  it('tells the page when a manager picks a different sprint length', async () => {
    await render(team());

    select().value = '3';
    select().dispatchEvent(new Event('change', { bubbles: true }));

    expect(changes).toEqual([3]);
  });

  it('does not report a change when the same length is picked again', async () => {
    await render(team());

    select().value = '2';
    select().dispatchEvent(new Event('change', { bubbles: true }));

    expect(changes).toEqual([]);
  });

  it('lets only a Project Manager change the sprint length', async () => {
    await render(team(), false);

    expect(select().disabled).toBe(true);
  });
});
