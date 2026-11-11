# AiSdlc

AI-assisted software development lifecycle: AI drafts requirements analysis, architecture, development tasks and QA test cases; named reviewers approve each stage. Portfolio project; all data is synthetic.

<!-- Replace <owner>/<repo> with the GitHub path once the remote exists. -->
[![ci](https://github.com/<owner>/<repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<owner>/<repo>/actions/workflows/ci.yml)

![AiSdlc main screen](docs/screenshots/main-light.png)

## What it does
- Upload a requirements document (`.docx` or `.txt`) and a team sheet (`.xlsx` with name, role, skills, capacity, availability); files are validated, hashed and versioned.
- AI analyses the requirements, drafts an architecture plan, breaks it into development tasks and generates QA test cases.
- Four human review gates (analysis, architecture, tasks, test cases) with role-based approval; reviewers can regenerate any output with written feedback.
- Tasks are scheduled across sprints (2, 3 or 4 weeks, default 2; 8 working hours a day) and spill into later sprints until everything is assigned; the team page shows each member's utilisation as a progress bar.
- Full audit trail of uploads, AI calls, decisions and edits.

See [docs/spec.md](docs/spec.md) for roles, gates and rules.

## Architecture

![Container view](docs/diagrams/containers.svg)

| Diagram | What it shows |
| --- | --- |
| [Containers](docs/diagrams/containers.svg) | Web app, API, SQLite, file storage and the AI model |

Services and layers are described in [docs/architecture.md](docs/architecture.md). Key decisions are recorded in [docs/adr](docs/adr).

## Tech stack
.NET 10 · ASP.NET Core Minimal APIs · EF Core · SQLite · ASP.NET Core Identity + JWT · Angular · Microsoft.Extensions.AI with Azure AI Foundry · Aspire · OpenTelemetry

## Run it locally

> Slices land one at a time; sections below become fully true as the API and web app are merged.

### Prerequisites
| Tool | Version | Check with |
| --- | --- | --- |
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 22 LTS or newer | `node --version` |
| Aspire CLI (optional) | latest | `aspire --version` |

No Docker is needed: the database is SQLite and files are stored on local disk.

### 1. Clone
```bash
git clone https://github.com/<owner>/<repo>.git
cd <repo>
```

### 2. Set secrets (never committed)
The app runs offline with a fake AI provider. To use Azure AI Foundry set:
```bash
dotnet user-secrets --project src/AiSdlc.AppHost set "Parameters:foundry-endpoint" "<value>"
dotnet user-secrets --project src/AiSdlc.AppHost set "Parameters:foundry-key" "<value>"
```
| Secret | Needed for | Where to get it |
| --- | --- | --- |
| `Parameters:foundry-endpoint` | AI text generation | Azure AI Foundry portal, your deployment's endpoint |
| `Parameters:foundry-key` | AI text generation | Azure AI Foundry portal, Keys and Endpoint |

### 3. Run
```bash
dotnet run --project src/AiSdlc.AppHost
```
| What | URL |
| --- | --- |
| Web app | <http://localhost:4200> |
| API (OpenAPI UI) | printed in the AppHost console |

### 4. Sign in with a test user
Seeded in Development only; passwords are listed here once the auth slice lands.

### 5. Run the tests
```bash
dotnet test
npm run test:ci --prefix src/web/aisdlc-web
```

### Troubleshooting
- **Port 4200 busy**: stop the other Angular dev server and run again.
- **HTTPS certificate warning**: run `dotnet dev-certs https --trust` once.

## Deploy (optional)
See [docs/deploy.md](docs/deploy.md) once it exists. Deployment is manual and isn't needed to run or review the code.

## Licence
MIT
