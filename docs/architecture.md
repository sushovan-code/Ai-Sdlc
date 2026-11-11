# AiSdlc architecture

Follows `docs/standards/architecture.md`. Single module, so projects are `AiSdlc.<Layer>`.

## Services and data stores
| Part | Technology | Notes |
| --- | --- | --- |
| Web | Angular (standalone, signals), Angular Material | `src/web/aisdlc-web`, port 4200 |
| API | ASP.NET Core Minimal APIs, .NET 10 | Endpoint groups per feature, Problem Details, JWT bearer |
| Identity | ASP.NET Core Identity + JWT | Roles: ProjectManager, Architect, Developer, QA, Admin |
| Database | SQLite via EF Core | One file under `App_Data/`, created and seeded on start in Development |
| File storage | `IFileStorage`, local-disk adapter | Folder under `App_Data/files`, never served statically |
| AI | `IChatClient` (Microsoft.Extensions.AI) | `Ai:Provider` = `Fake` (tests, offline) or `OpenAI` (gpt-5.4-mini for text generation) |
| Orchestration | Aspire AppHost | Starts API and web; no Docker needed |

## Layers
Domain (project, gate, artifact version, team member, sprint, scheduler) <- Application (use cases by feature, ports, AI agents) <- Infrastructure (EF Core, Identity, storage, docx/xlsx parsing, Fake and OpenAI chat clients) <- Api (endpoints, policies, composition root).

## Main flows
1. Upload: validate type, size and signature, hash, store a new file version, audit.
2. Generate (built for gate 1, the analysis): the handler checks the stage and that the caller is a Project Manager, reads the latest requirements, and the governance guard masks personal data and secrets. The specialist agent drafts, the retrieval agent attaches evidence quotes, the critic and governance agents annotate, and the result is stored as a new artifact version in `PendingReview`. Every model call is audited. See ADR 0003.
3. Review: the stage's reviewer role (Project Manager, Architect, Project Manager, QA for gates 1 to 4) approves, rejects, edits or regenerates with feedback. Approval moves the gate and unlocks the next stage. Gates 2 to 4 share one generic pipeline (ADR 0004).
4. Plan sprints: after gate 2 the tasks agent proposes tasks; the deterministic scheduler assigns them across sprints using member capacity, and the team page shows the booked hours.
5. Wrap-up: traceability from requirements to tests, the project audit trail and a Markdown report.

## Cross-cutting
Deny-by-default authorisation with per-endpoint policies and project-membership checks; append-only audit log; structured logs without PII; rate limiting on auth and write endpoints; security headers and strict CORS.
