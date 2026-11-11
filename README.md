# AiSdlc

AI-assisted software development lifecycle: AI drafts requirements analysis, architecture, development tasks and QA test cases; named reviewers approve each stage. Portfolio project; all data is synthetic.

<!-- Replace <owner>/<repo> with the GitHub path once the remote exists. -->
[![ci](https://github.com/<owner>/<repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<owner>/<repo>/actions/workflows/ci.yml)

![AiSdlc main screen](docs/screenshots/main-light.png)

## What it does
- Upload a requirements document (`.docx` or `.txt`) and a team sheet (`.xlsx` with name, role, skills, capacity, availability); files are validated, hashed and versioned.
- AI analyses the requirements into functional and non-functional requirements, questions and risks, each backed by quotes from the document; the Project Manager approves, rejects, or regenerates it with written feedback (built). The same flow then drafts the architecture plan (Software Architect), the development tasks (Project Manager) and the QA test cases (QA). Each reviewer can edit a draft by hand.
- Four human review gates (analysis, architecture, tasks, test cases), each approved by its own role; reviewers can regenerate any output with written feedback or correct it by hand.
- A wrap-up step traces every requirement to its design, tasks and tests, shows the project audit trail and downloads a Markdown report.
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

No Docker and no database server are needed: the database is SQLite and uploaded files are stored on local disk.

### Prerequisites
| Tool | Version | Check with |
| --- | --- | --- |
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 22 or newer | `node --version` |
| Aspire CLI (optional, for `aspire run`) | latest | `aspire --version` (install: `dotnet tool install --global Aspire.Cli`) |

### 1. Clone and install the web packages (once)
```bash
git clone https://github.com/sushovan-code/Ai-Sdlc.git
cd Ai-Sdlc
cd src/web/aisdlc-web
npm install
cd ../../..
```

### 2. Secrets
No secret is needed to try the app. By default `Ai:Provider` is `Fake`, an offline model that drafts a simple analysis from your document so the whole flow works without a key or a network. To use the real model, set an OpenAI key as a user secret from your own terminal (never committed, never pasted into chat or a config file).

With Aspire (option A), one secret switches the API to OpenAI:
```bash
dotnet user-secrets --project src/AiSdlc.AppHost set "Parameters:openai-api-key" "<your key>"
```
Standalone (option B), set the provider and key on the API project:
```bash
dotnet user-secrets --project src/AiSdlc.Api set "Ai:Provider" "OpenAI"
dotnet user-secrets --project src/AiSdlc.Api set "Ai:ApiKey" "<your key>"
```
| Secret | Needed for | Where to get it |
| --- | --- | --- |
| `Parameters:openai-api-key` (AppHost) or `Ai:ApiKey` (API) | AI text generation with the real model (`Ai:Model`, default `gpt-5.4-mini`) | OpenAI platform, API keys |

If `Ai:Provider` is `OpenAI` and no key is set, the API refuses to start and says so.

### 3. Run it, option A: with Aspire (one command)
From the repository root:
```bash
dotnet run --project src/AiSdlc.AppHost --launch-profile https
```
or, if the Aspire CLI is installed, `aspire run`.

This starts the API and the Angular app together and prints a link to the Aspire dashboard (logs, traces and resource status). Open **http://localhost:4200**. Press **Ctrl+C** to stop everything.

> Use the `https` launch profile. The `http` profile makes Aspire refuse to start unless `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` is set.

### 3. Run it, option B: standalone (two terminals)
Start the API first, then the web app.

Terminal 1, API (listens on http://localhost:5073):
```bash
dotnet run --project src/AiSdlc.Api --launch-profile http
```

Terminal 2, web app:
```bash
cd src/web/aisdlc-web
npm start
```
Open **http://localhost:4200**. The dev server forwards `/api` to http://localhost:5073 (see `src/web/aisdlc-web/proxy.conf.js`), so there is nothing to configure. Stop each terminal with **Ctrl+C**.

### Where things are
| What | URL or path |
| --- | --- |
| Web app | <http://localhost:4200> |
| API | <http://localhost:5073> (Aspire may assign its own port; the dashboard shows it) |
| API description (OpenAPI, Development only) | <http://localhost:5073/openapi/v1.json> |
| Aspire dashboard (option A only) | printed in the console |
| Database and uploaded files | `src/AiSdlc.Api/App_Data/` (delete this folder for a clean start) |

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

### 5. Try it
1. Sign in as `pm@demo.local`, enter a project name and select **Create project**. A notification confirms each action in the top-right corner.
2. On **Files & team**, upload a requirements file (`.docx` or `.txt`) and a team sheet (`.xlsx`). Uploading again adds a new version.
3. Under **Team**, pick people from the suggestions (or filter by name) and select **Add**. Only people added to a project can see it.
4. Use **Next** and **Previous** (or the stepper) to move between stages. **Next** stays disabled, with the reason shown beside it, until the stage is complete and the review gate that closes it is approved by the right role.
5. After both files are uploaded, open **Analysis** and select **Generate with AI**. Read the draft (requirements with evidence quotes, questions, risks and an automatic review), type a comment and select **Regenerate** to get a new version, or select **Approve** to pass gate 1. **Reject** needs a reason. Approving unlocks the next stage and locks the requirements.
6. Add `architect@demo.local` and `qa@demo.local` to the project (Project members). Sign in as the Architect, open **Architecture** and generate, edit and approve it (gate 2). Back as the Project Manager, open **Tasks**: the scheduler places the tasks across sprints and the team panel shows booked hours; approve (gate 3). Sign in as QA, generate and approve the **Test cases** (gate 4). Each stage only shows its actions to the role that reviews it.
7. Open **Wrap-up** (unlocks after gate 4): the traceability table, the audit trail and **Download report**.
8. Sign out and sign in as `architect@demo.local`: the project is visible, files can be downloaded but not uploaded or deleted. Sign in as `qa@demo.local` (not added): the project is not listed, and opening its URL shows "Project not found".

Files are checked for type, size (10 MB), signature and structure; macro-enabled files and mismatched content are rejected.

### The team sheet
The team template is an `.xlsx` whose first sheet has these columns (in any order, headers can sit below a title):

| Name | Role | Skills | Capacity | Availability |
| --- | --- | --- | --- | --- |
| Asha Rao | Developer | C#, ASP.NET Core, SQL, EF Core | 40 | 30 |
| Meera Iyer | QA | Testing, Selenium, API Testing | 40 | 40 |

- **Capacity** and **Availability** are hours for a **2-week window**. Availability cannot exceed Capacity, and Capacity cannot exceed 80 hours (10 working days of 8 hours).
- Skills are separated by commas or semicolons. Each person must be listed once.
- A sheet with any bad row is refused as a whole, and the message names every problem by row so it can be fixed in one go.
- The **sprint length** is set per project: 2 weeks (default), 3 or 4. A person's hours per sprint are their Availability × (weeks ÷ 2). Work that does not fit in the first sprint spills into the next until everything is assigned.
- People on the sheet are capacity to plan with, not login accounts. The **Team capacity and sprints** panel shows each person's booked hours against capacity as a progress bar (nothing is booked until tasks are scheduled in a later release).

### 6. Run the tests
```bash
dotnet test
cd src/web/aisdlc-web
npm run test:ci
npm run lint
```

### Troubleshooting
- **Port 4200 or 5073 is busy**: another copy is still running. Stop it (Ctrl+C in its terminal) and start again.
- **Aspire says the `applicationUrl` must be https**: you used the `http` profile. Use `--launch-profile https`.
- **HTTPS certificate warning**: run `dotnet dev-certs https --trust` once.
- **Sign-in says "Cannot reach the server"**: the API is not running (option B) or is still starting; wait a few seconds.
- **Regenerate the typed API client** after changing endpoints: start the API, then `npm run api:types --prefix src/web/aisdlc-web`.
- **Start with clean data**: stop the app and delete `src/AiSdlc.Api/App_Data/`.

## Deploy (optional)
See [docs/deploy.md](docs/deploy.md) once it exists. Deployment is manual and isn't needed to run or review the code.

## Licence
MIT
