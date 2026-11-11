## Summary
Slice 8: the wrap-up views, traceability, the project audit trail and the report.

- **Traceability**: for every requirement, the architecture items, tasks and test cases that trace back to it, following references through the stages. The page says how many requirements still have a gap.
- **Audit trail**: a paged, newest-first view of everything recorded for a project (uploads, AI calls, decisions, edits). It is read through a new `IAuditQuery` port, separate from the write-only audit log. AI calls for the analysis now carry their project id so they show up here. Because the trail names people and uploaded files, it is readable by the Project Manager, Architect and QA only; the page hides it for other roles.
- **Report**: a Markdown download of each stage's current output with its review decisions. The file name is built from the project name and never used as a path. Each download is audited and shares the write rate limit.
- **UI**: a Wrap-up page (the sixth step, unlocked after gate 4). The unused "coming soon" placeholder panel is removed.
- Decisions: ADR 0004 (README and architecture notes updated).

## How it was tested
- [x] Backend unit and integration tests pass (`dotnet test`): the end-to-end lifecycle test also checks traceability, the audit trail and the report
- [x] Frontend tests pass (`npm run test:ci`): 162; lint and build clean
- [x] Security review run (security-reviewer agent); confirmed findings fixed
- [ ] Runs end to end locally with `aspire run` in light and dark themes: **not yet checked visually**

## Screenshots
Not captured yet.

## Risks and follow-ups
- The audit trail is found by a text prefix on the details; a project column would be better if the trail grows large.
- Retention and deletion (an administrator list of projects with a delete action) is the next slice.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
