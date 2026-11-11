# 0004. One generic stage pipeline for gates 2 to 4, and the wrap-up views

Status: accepted (slices 5 to 8)

## Context
After the analysis, three more AI outputs need the same treatment: the architecture plan (reviewed by the Software Architect), the development tasks (Project Manager) and the QA test cases (QA). Each must be versioned, reviewable, editable by the reviewer and traceable, and tasks must be placed across sprints by the existing deterministic scheduler. The product also needs traceability, an audit view and a report once the lifecycle is done.

## Decision
- **The Project aggregate is stage-generic.** `AddDraft`, `Approve` and `Reject` take an `ArtifactStage`. `StageRules` (Domain) holds the lifecycle order in one place: which project stage a draft needs, where approval leaves the project, and which role reviews it. The analysis methods stay as thin wrappers.
- **One content shape.** Architecture, tasks and test cases all store a `StageContent`: a summary and titled sections of items (id, title, text, tags, refs). One shape gives one review page, one edit flow, one report and one traceability engine. `refs` point at earlier ids (requirements, architecture items, tasks).
- **One generator.** `StageGenerator` builds the prompt from the approved earlier stages (`StageSourceBuilder`), masks reviewer feedback, caps tokens, validates the JSON, retries once, drops citations to ids that do not exist and flags items nothing traces back to. Earlier output is passed inside neutralised tags as data.
- **The model proposes tasks, code schedules them.** The tasks answer is parsed as typed tasks (hours, priority, skills, dependencies) and `TaskPlanner` runs `SprintScheduler` with the team's sprint capacity. The assignments are stored with the draft, so the team page shows booked hours from the newest task plan still in play.
- **Role per stage.** The handlers check the caller holds the stage's reviewer role and is a project member. Endpoints use the authenticated-user policy and the handler enforces the role, because the role depends on the stage in the route. The Administrator never reviews a gate.
- **Hand edits are new versions.** A reviewer's correction is saved as a version marked `Human`, waiting for review like any other. The AI draft and every edit stay in the history. Edits do not re-run the scheduler, so a changed task label does not move hours on the team page.
- **Wrap-up views are read models.** Traceability follows references through the stages (a test that cites a task counts for the requirements that task serves). The audit trail is read back through `IAuditQuery`, a separate port from the write-only `IAuditLog`; entries name their project at the start of the details text. The report is built as Markdown from the newest version still in play for each stage, including review decisions.

## Consequences
- Adding a stage later means a new `ArtifactStage`, rules, a prompt and an item shape, not new plumbing.
- Searching the audit trail by a text prefix is simple but not indexed by project; a project column would be better if the trail grows large.
- The in-process generation lock still assumes a single running instance.
- Test coverage for these stages is light by choice: one end-to-end test through all four gates and the wrap-up, plus the earlier unit tests. More unit tests for the planner and the traceability engine are a sensible follow-up.
