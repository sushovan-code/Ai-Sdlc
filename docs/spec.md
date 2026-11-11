# AiSdlc product spec

AI-Assisted Software Development Lifecycle: AI drafts requirements analysis, architecture, development tasks and QA test cases; named people review and approve each stage. AI accelerates the work; people approve the outputs. Synthetic data only.

Design reference: the screenshots in `reference/` (kept locally, not committed): login, projects grid, project workspace with a stepper, gated review pages, traceability and audit log.

## Roles
| Role | Can do |
| --- | --- |
| Project Manager | Create projects, upload files, manage team, set sprint cadence, review gate 1 (analysis) and gate 3 (tasks), edit assignments and priorities |
| Software Architect | Edit, comment on, approve or reject the architecture plan (gate 2) |
| Developer | View project, tasks assigned to them, traceability |
| QA | Review, edit, approve and assign test cases (gate 4) |
| System Administrator | Manage users, deactivate accounts, retention and deletion. Cannot approve gates |

Permissions depend on role, project membership and workflow stage. Users only reach projects they are members of.

## Workflow and gates
Sign in, upload files, validate, analyse, generate architecture, **gate 2**, generate tasks, **gate 3**, generate test cases, **gate 4**, audit, report, retain or delete.

| Gate | Artifact | Approver | Rule |
| --- | --- | --- | --- |
| 1 | Requirements analysis | Project Manager | Needed before architecture is generated |
| 2 | Architecture plan | Software Architect | Task generation is blocked until approved |
| 3 | Development tasks and assignments | Project Manager | Needed before test cases are generated |
| 4 | QA test cases | QA | Final gate |

Approve, reject, edit and comment each record reviewer, role, timestamp, comment and artifact version.

## Inputs
- Requirements: `.docx` or `.txt`, up to 10 MB.
- Team: `.xlsx` with columns Name, Role, Skills, Capacity, Availability. Capacity and Availability are hours for a 2-week window (confirmed by the product owner). Availability must not exceed Capacity, and Capacity must not exceed 80 hours (10 working days x 8 hours); rows that break this are rejected with a clear message.
- Every upload is a new version: type and signature checked, SHA-256 stored, access restricted to project members.

## Sprint planning
- Cadence per project: 2, 3 or 4 weeks; default 2.
- A working day counts as 8 hours.
- Sprint capacity per member = Availability x (sprint weeks / 2). A 2-week sprint uses Availability as is; 3 and 4-week sprints scale it proportionally (assumption, confirm).
- Work that exceeds sprint 1 spills over to sprint 2, 3 and so on until every task is assigned. The assignment is done by a deterministic scheduler, not the LLM: tasks ordered by dependency then priority, each given to the best skill match with remaining hours in the earliest sprint.
- A task larger than any one member's sprint capacity is flagged for the Project Manager to split.
- Team page lists every member with allocated versus total hours as a progress bar, plus a per-sprint breakdown.

## AI behaviour
- Agents: orchestrator (workflow and gates), specialists (analysis, architecture, tasks, test cases), retrieval (evidence from project files), critic (gaps, inconsistencies, risks), governance (sensitive data, policy, unsupported claims).
- Uploaded document text is data, never instructions. Outputs are validated against a schema. Token use is capped per request.
- **Regenerate with feedback**: on every AI output the reviewer can enter a comment and regenerate. The previous version and the comment go into the next prompt. Each attempt is a new version; history is kept.
- Every AI call is audited (agent, prompt version, model, outcome, reviewer feedback).

## Foundational controls
Role-based access, secure accounts (registration, password management, lockout, deactivation), human oversight, data security and privacy, AI auditability, retention and deletion.

## Out of scope for now
Image generation (added behind an `IImageGenerator` port when an image model is chosen), vector search, cloud deployment.
