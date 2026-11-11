## Summary
Slices 5 to 7: the architecture, task and test-case gates (gates 2 to 4) on one generic stage pipeline.

- **Generic stages**: the `Project` aggregate drafts, approves and rejects any stage in lifecycle order; `StageRules` holds the order and the reviewer role in one place. Gate 2 is approved by the Software Architect, gate 3 by the Project Manager, gate 4 by QA. The Administrator never approves.
- **One content shape** (summary plus sections of items with refs back to earlier ids) gives one review page, one edit flow and one traceability engine. Decisions: ADR 0004.
- **Generation** drafts each stage from the approved earlier stages, with the same safeguards as the analysis: masked feedback, neutralised prompt tags, token caps, schema validation with one retry, unknown references dropped, untraced items flagged, every call audited, one generation per project at a time.
- **Tasks are placed by the deterministic scheduler**, not the model. The assignments are stored with the draft, so the team page now shows booked hours from the newest task plan.
- **Hand edit**: the stage reviewer can correct a draft; it is saved as a new version marked as human, waiting for review.
- **UI**: one simple review page for all three stages (generate, read, edit, regenerate with feedback, approve or reject), shown only to the reviewing role; a sixth "Wrap-up" step is added to the stepper.
- **Security review fixes included**: the edit endpoint checks access first and refuses incomplete bodies with 400, ids and references have length limits, the edit body is capped at 512 KB, and the prompt input is capped.

## How it was tested
- [x] Backend unit and integration tests pass (`dotnet test`): 320 unit, 130 integration, including an end-to-end test through all four gates with each role, a wrong-role refusal, a draft-order refusal, an edit and a malformed-edit case
- [x] Frontend tests pass (`npm run test:ci`): 162; lint and build clean
- [x] Security review run (security-reviewer agent); confirmed findings fixed
- [ ] Runs end to end locally with `aspire run` in light and dark themes: **not yet checked visually**

## Screenshots
Not captured yet.

## Risks and follow-ups
- Hand-editing a task does not re-run the scheduler, so booked hours on the team page keep following the last plan.
- The generation lock is in-process (single instance).
- Tests for the task planner and the stage generator are light by choice; more unit tests are a sensible follow-up.
- This branch also carries the already-merged slice 4 commits individually; the content is identical to `main`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
