# 0002. Team sheet as a capacity roster, and a deterministic sprint scheduler

Status: accepted (slice 3)

## Context
The team sheet lists people with skills and hours. The task slice will assign AI-proposed tasks to them across sprints. The use case needs assignments a Project Manager can trust and explain, and sprints of 2, 3 or 4 weeks.

## Decision
- **Roster, not accounts.** People on the sheet are stored as `TeamMember` rows per sheet version. They are separate from login users (who gain access through project membership). The current roster is the people on the newest sheet that has not been deleted, so deleting a bad upload brings back the previous roster.
- **Validated at upload.** The sheet is read behind `ITeamSheetReader` (ClosedXML adapter) and checked by `TeamSheetValidator` in Application. A sheet with any bad row is refused whole, with every problem listed by row. Capacity and Availability are hours per 2 weeks; Availability <= Capacity <= 80 (10 days x 8 hours).
- **Sprint hours.** Per-sprint capacity is Availability x (sprint weeks / 2). The working day (8 hours) and the allowed lengths (2, 3, 4; default 2) are constants in `SprintRules`.
- **The LLM proposes, code schedules.** `SprintScheduler` (Domain, pure) orders tasks by dependency then priority then input order, gives each to the best skill match with room in the earliest sprint, and spills into later sprints. A task is never split. Tasks it cannot place are returned with a reason (no matching skill, exceeds any sprint, dependency loop, and so on) for the Project Manager to resolve. Ties are broken by name, so the same input always gives the same plan.
- **Utilization.** `UtilizationCalculator` reports booked hours against capacity over the sprints the plan spans. Until tasks are scheduled it is zero.

## Consequences
- Plans are explainable and testable without a model. The scheduler is covered by unit tests for spill-over, cadence, dependencies, priority, skills and every failure reason.
- A task larger than anyone's sprint cannot be placed and must be split by a person; automatic splitting is a possible later extension.
- ClosedXML (MIT) is a new dependency; it brings SixLabors.Fonts 1.0.0 (Apache-2.0 at that version, so no commercial licence needed). Re-check licences when upgrading either.
- Sheets are parsed fully in memory. The upload path already limits file size (10 MB), archive entry count and declared uncompressed size before parsing.
