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
| AI | `IChatClient` (Microsoft.Extensions.AI) | `Ai:Provider` = `Fake` (tests, offline) or `Foundry` |
| Orchestration | Aspire AppHost | Starts API and web; no Docker needed |

## Layers
Domain (project, gate, artifact version, team member, sprint, scheduler) <- Application (use cases by feature, ports) <- Infrastructure (EF Core, Identity, storage, docx/xlsx parsing, Foundry adapter, agents) <- Api (endpoints, policies, composition root).

## Main flows
1. Upload: validate type, size and signature, hash, store a new file version, audit.
2. Generate: orchestrator checks the gate state and the caller's role, retrieval agent gathers evidence, specialist agent drafts, critic and governance agents annotate, the result is stored as a new artifact version in `PendingReview`.
3. Review: an authorised approver approves, rejects, edits or regenerates with feedback. Approval moves the gate and unlocks the next stage.
4. Plan sprints: after gate 2 the tasks agent proposes tasks; the scheduler assigns them across sprints using member capacity.

## Cross-cutting
Deny-by-default authorisation with per-endpoint policies and project-membership checks; append-only audit log; structured logs without PII; rate limiting on auth and write endpoints; security headers and strict CORS.
