# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## State of this repo
This is a **repo template / kit** for portfolio-grade .NET + Angular/React apps, not yet a product. There is no `src/`, `tests/`, `docs/spec.md` or `docs/architecture.md`, and `CLAUDE.md`, `README.md` and `ci.yml` still contain `<Product>`, `<owner>/<repo>` and `<product>` placeholders. When instantiating a product, replace every placeholder (including `WEB_DIR` in `.github/workflows/ci.yml`; delete the frontend CI steps if there is no frontend) and create the spec and architecture docs that the `@docs/...` imports below point to. The directory is not a git repository yet. `reference/` holds unrelated WhatsApp screenshots; ignore it.

<Product>: <one-line purpose>. Portfolio project; synthetic data only.

## Stack
.NET 10 · <frontend> · <auth> · Aspire · see @docs/architecture.md for services and data stores.

## Commands
- Run everything locally: `aspire run` (AppHost: `src/<Product>.AppHost`; needs Docker running)
- Backend tests: `dotnet test` · single test: `dotnet test --filter "FullyQualifiedName~CreateTransferHandlerTests.Handle_InsufficientFunds_ReturnsFailure"`
- Frontend tests: `npm run test:ci --prefix src/web/<product>-web` (CI and README use `test:ci`)
- Format: `dotnet format` · lint: `npm run lint --prefix src/web/<product>-web`
- Render diagrams: `pwsh -File scripts/render-diagrams.ps1` (needs Node for `npx` Mermaid CLI, plus Python `diagrams` and Graphviz for `deployment-*.py`)
- Secrets: `dotnet user-secrets --project src/<Product>.AppHost set "Parameters:<name>" "<value>"`

CI (`.github/workflows/ci.yml`) runs restore, Release build, `dotnet format --verify-no-changes --severity error`, tests with coverage, then frontend `npm ci`, lint, `test:ci`, build. It also runs a diagrams job and a gitleaks scan on PRs.

## Things that span files
- **Warnings don't fail the build** (`TreatWarningsAsErrors=false` in `Directory.Build.props`) despite the "warnings as errors" wording in the global defaults. Repo standards win: fix warnings your change introduces; don't hide them with `NoWarn`/`#pragma` without a comment. `CS1591` (missing XML docs) is intentionally suppressed.
- **Diagrams are code.** Sources in `docs/diagrams/` (`*.mmd` Mermaid, `*.py` Python `diagrams`) must be committed together with their rendered `*.svg`/`*.png`; `scripts/check-diagrams.ps1` fails the PR otherwise, and warns if `AppHost/`, `infra/` or `Contracts/` changed without any diagram change. README's Architecture section links these images at fixed paths.
- **Layering** (Domain ← Application ← Infrastructure ← Api; dependencies point inward) with swappable adapters chosen by configuration (`Cache:Provider`, `Ai:Provider`, `Storage:Provider`). The port table in `docs/standards/architecture.md` is the source of truth for which abstraction to use (HybridCache, `IChatClient`, `VectorStore`, `TimeProvider`, ...).
- **Vertical slices**: code is grouped by feature (`Application/Transfers/CreateTransfer/...`), one public type per file; project and namespace layout is in `docs/standards/naming-conventions.md`.
- **Workflow is slash-command driven** (`.claude/commands/`): `/new-feature` (plans, then stops for approval; no tests yet) → `/add-tests` → `/update-diagrams` if architecture changed → `/prepare-pr`. Also `/fix-bug` and `/deploy-plan` (prepares Terraform/pipeline changes, never applies). Subagents in `.claude/agents/`: `security-reviewer`, `ui-reviewer`, `test-writer`.
- **Permissions** (`.claude/settings.json`) allow build/test/format/ef-migrations-add/aspire/git status-add-commit and `terraform plan`; they deny `terraform apply/destroy`, `git push --force`, `git reset --hard`, and reading `.env*` and `secrets.json`.

## Standards (read the relevant file before working in that area)
- Coding standards, best practices and design principles (always applies): @docs/standards/coding-standards.md
- Development workflow and definition of done: @docs/standards/development-workflow.md
- Naming conventions: @docs/standards/naming-conventions.md
- Architecture and loose coupling: @docs/standards/architecture.md
- Testing: @docs/standards/testing.md
- Security: @docs/standards/security.md
- UI, design and theming: @docs/standards/ui-and-theming.md
- Caching: @docs/standards/caching.md

## Always
- When you add, remove or rewire a service, data store, cache, queue, external system or main flow, update `docs/diagrams/` and README.md's Architecture section (`/update-diagrams`).
- When you add a secret, prerequisite, port or test user, update README.md's "Run it locally" section.

## Product spec
@docs/spec.md
