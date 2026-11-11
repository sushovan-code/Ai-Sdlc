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
.NET 10 · ASP.NET Core Minimal APIs · EF Core · SQLite · ASP.NET Core Identity + JWT · Angular · Microsoft.Extensions.AI with OpenAI · Aspire · OpenTelemetry

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
The app runs offline with a fake AI provider. To use OpenAI for text generation (arrives with the AI slices) set:
```bash
dotnet user-secrets --project src/AiSdlc.AppHost set "Parameters:openai-api-key" "<value>"
```
| Secret | Needed for | Where to get it |
| --- | --- | --- |
| `Parameters:openai-api-key` | AI text generation (model `gpt-5.4-mini`) | OpenAI platform, API keys |

### 3. Run
```bash
dotnet run --project src/AiSdlc.AppHost --launch-profile https
```
(or `aspire run` if the Aspire CLI is installed). Run `cd src/web/aisdlc-web && npm install` once first. Press Ctrl+C to stop.
| What | URL |
| --- | --- |
| Web app | <http://localhost:4200> |
| API (OpenAPI document, Development only) | <http://localhost:5073/openapi/v1.json> |
| Aspire dashboard (logs, traces) | printed in the AppHost console |

### 4. Sign in with a test user
Seeded in Development only (local data, not real credentials). All five share one password:

| User | Password | Role |
| --- | --- | --- |
| `pm@demo.local` | `Demo#Passw0rd!` | Project Manager |
| `architect@demo.local` | `Demo#Passw0rd!` | Architect |
| `dev@demo.local` | `Demo#Passw0rd!` | Developer |
| `qa@demo.local` | `Demo#Passw0rd!` | QA |
| `admin@demo.local` | `Demo#Passw0rd!` | Administrator (manages users and roles) |

You can also create an account on the sign-in page. New accounts start as Developers; sign in as the administrator and open **Users** to change a role.

### 5. Run the tests
```bash
dotnet test
npm run test:ci --prefix src/web/aisdlc-web
```

### Troubleshooting
- **Port 4200 or 5073 busy**: stop the other process using it and run again.
- **Regenerate the typed API client** after changing endpoints: start the API, then `npm run api:types --prefix src/web/aisdlc-web`.
- **HTTPS certificate warning**: run `dotnet dev-certs https --trust` once.

## Deploy (optional)
See [docs/deploy.md](docs/deploy.md) once it exists. Deployment is manual and isn't needed to run or review the code.

## Licence
MIT
