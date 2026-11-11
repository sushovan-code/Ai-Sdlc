# 0003. AI analysis pipeline, versioned drafts and the gate 1 review

Status: accepted (slice 4)

## Context
Gate 1 needs an AI draft of the requirements analysis that a Project Manager can trust enough to review: every claim traceable to the document, safe against hostile document text, repeatable in tests, and with a full history of drafts and decisions. The product must also run with no network or key.

## Decision
- **One port, two adapters.** Application depends on `IChatClient` (Microsoft.Extensions.AI). `Ai:Provider` selects `Fake` (offline, deterministic, the default) or `OpenAI` (`gpt-5.4-mini`) in a small factory. The API key is a user secret; the app refuses to start on `OpenAI` without one.
- **A small pipeline of agents**, each a plain class with one job:
  - *Governance guard* (deterministic): masks emails, phone numbers, Luhn-valid card numbers and secrets before text leaves the system, and flags wording that looks like instructions to the model. The document stays data.
  - *Chunker*: splits the text into numbered chunks (C1, C2, ...) that the model must cite.
  - *Specialist*: asks for one JSON object, caps output tokens, validates the answer against the schema and retries once with the problem described.
  - *Retrieval*: checks every cited chunk id against the real document, drops invented citations and attaches the quoted text as evidence. Requirements with no valid citation are flagged as unsupported.
  - *Critic*: lists gaps and inconsistencies for the reviewer. It is advice: if it fails, the draft still goes ahead with a note saying the review is missing.
- **Versioned, never overwritten.** Each attempt is an `ArtifactVersion` (content JSON, notes, model, prompt version, the feedback that asked for it, the version it was based on). A new draft supersedes any draft still waiting. Regenerating sends the previous draft and the reviewer comment as data inside tags.
- **The aggregate guards the gate.** `Project` owns the versions and the rules: drafting is allowed only after both files are uploaded and before approval; only the newest pending version can be approved or rejected; approval moves the stage to `AnalysisApproved` and locks the requirements so the approved analysis stays true to them. Each decision stores reviewer, role, time and comment. Application handlers map domain outcomes to Problem Details (409 for stage and version conflicts, 503 when the model is unreachable).
- **Audit.** Every model call is audited (agent, model, prompt version, tokens, outcome), plus generation, approval and rejection.
- **Roles.** Project Managers generate and decide at gate 1; every project member can read.

## Consequences
- The whole flow runs and is tested offline with the fake client and a scripted client; a real-model evaluation suite is a later, separate job.
- Masking changes what the model sees, so a requirement about a specific email address is described by its placeholder. The reviewer is told how many items were masked.
- The text sent to the model is capped (60,000 characters, about 6,000 output tokens). Longer documents are refused with a clear message; chunked multi-pass analysis is a possible extension.
- Drafts store JSON, so changing the content shape later needs a version field or a migration of stored drafts.
- Later gates (architecture, tasks, test cases) reuse `ArtifactVersion` and the same agent shape with a new `ArtifactStage` and prompts.
- The injection check in the governance guard is a heuristic hint, not a control. The real defences are that document text is data inside neutralised tags, the model has no tools, output is schema-validated, and a person approves at the gate.
- One generation runs per project at a time, and a decision is refused while one runs (`AnalysisGenerationLock`). It is in-process, which fits the single-instance app; several instances would need a shared lock or a database concurrency token.
- Generation has its own rate limit (default 5 per minute per user) because each call costs money.
