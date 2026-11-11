## Summary
Slice 4: AI drafts the requirements analysis and the Project Manager reviews it at gate 1.

- **Generate**: a Project Manager generates a draft from the latest requirements file. A small pipeline runs: a governance guard masks personal data and secrets, the text is split into numbered chunks, a specialist agent drafts structured JSON (validated, one retry), a retrieval agent attaches quotes from the document as evidence, and a critic adds review notes. Every model call is audited.
- **Regenerate with feedback**: one comment box. Each attempt is a new version; the previous draft and the comment go into the next prompt as data. History is kept.
- **Gate 1**: approve (optional note) or reject (reason required). Approving moves the project to `AnalysisApproved`, unlocks the next stage in the stepper and locks the requirements (upload and delete return 409).
- **AI provider** behind `IChatClient`: `Ai:Provider` is `Fake` (offline, default, no key needed) or `OpenAI` (`gpt-5.4-mini`). The AppHost passes the key only if the `openai-api-key` user secret is set.
- **UI**: one simple Analysis page with the Copilot-icon button (stand-in icon until the official SVG is supplied).
- **Hardening from the security review**: one generation per project at a time, a tighter AI rate limit, neutralised prompt delimiters, masked reviewer feedback, a capped `.docx` read.

New dependencies: `Microsoft.Extensions.Logging.Abstractions` (Application); `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI` and `DocumentFormat.OpenXml` (Infrastructure). Decisions: ADR 0003.

## How it was tested
- [x] Runs end to end locally with `aspire run` (light and dark themes): **not yet checked visually**; the API flow is covered by integration tests against the real HTTP pipeline with the fake model
- [x] Backend unit and integration tests pass (`dotnet test`): 320 unit, 126 integration
- [x] Frontend tests pass (`npm run test:ci`): 162; lint and build clean
- [x] Security review run (security-reviewer agent); confirmed findings fixed

## Screenshots
Not captured yet (needs the app running in light and dark).

## Risks and follow-ups
- The generation lock is in-process (single instance). Several instances would need a shared lock or a concurrency token.
- The injection check is a heuristic hint; the real defences are listed in ADR 0003.
- `schema.d.ts` was edited by hand for `ProjectStage`; regenerate it with `npm run api:types` once the API is running.
- `containers.svg` is rendered and committed with its source.
- Copilot icon is a stand-in; send the official SVG path to replace it.
- Pre-existing, not from this branch: `.btn-primary` uses literal colours instead of tokens.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
