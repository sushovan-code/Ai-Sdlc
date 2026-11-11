# <Product>

<one-line purpose>. Portfolio project; all data is synthetic.

<!-- Badges: CI status, licence. Replace <owner>/<repo>. -->
[![ci](https://github.com/<owner>/<repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<owner>/<repo>/actions/workflows/ci.yml)

<!-- One screenshot of the main screen, light theme. -->
![<Product> main screen](docs/screenshots/main-light.png)

## What it does
- <3 to 5 bullets: the user-facing features>

## Architecture

<!-- Kept current by /update-diagrams. The images are re-rendered at these fixed paths, so this section shows the latest diagrams without being edited; only the text below changes when services change. -->

![Container view](docs/diagrams/containers.svg)

| Diagram | What it shows |
| --- | --- |
| [System context](docs/diagrams/context.svg) | Who uses <Product> and which external systems it talks to |
| [Containers](docs/diagrams/containers.svg) | Frontend, API, workers, databases, cache and queues |
| [Login flow](docs/diagrams/sequence-login.svg) | Sign-in, token handling and logout |
| [Deployment on Azure](docs/diagrams/deployment-azure.png) | What runs where when deployed (optional stage 2) |

Key decisions are recorded in [docs/adr](docs/adr).

## Tech stack
.NET 10 · ASP.NET Core Minimal APIs · <frontend> · <auth> · <database> · Redis · SignalR · Aspire · OpenTelemetry

## Run it locally

### Prerequisites
| Tool | Version | Check with |
| --- | --- | --- |
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 22 LTS | `node --version` |
| Docker Desktop | running | `docker info` |
| Aspire CLI | latest | `aspire --version` (install: `dotnet tool install --global Aspire.Cli`) |

### 1. Clone
```bash
git clone https://github.com/<owner>/<repo>.git
cd <repo>
```

### 2. Set secrets (never committed)
Local containers (database, Redis<, Keycloak>) need no secrets; Aspire creates them. Only cloud services need values:
```bash
dotnet user-secrets --project src/<Product>.AppHost set "Parameters:<name>" "<value>"
```
| Secret | Needed for | Where to get it |
| --- | --- | --- |
| `<e.g. Parameters:foundry-key>` | <e.g. the AI assistant> | <e.g. Microsoft Foundry portal, Keys and Endpoint> |

The app starts without the optional ones; the features that need them show as unavailable.

### 3. Run
```bash
aspire run
```
Open the dashboard link printed in the terminal. From there:

| What | URL |
| --- | --- |
| Web app | <https://localhost:xxxx> |
| API (OpenAPI UI) | <https://localhost:xxxx/scalar> |
| Aspire dashboard (logs, traces, metrics) | printed by `aspire run` |

The first run downloads container images, so it takes a few minutes.

### 4. Sign in with a test user
| User | Password | Role |
| --- | --- | --- |
| `<demo-user>` | `<demo password, local only>` | <role> |

These users exist only in the local seed data.

### 5. Run the tests
```bash
dotnet test
npm run test:ci --prefix src/web/<product>-web
```

### Troubleshooting
- **Containers don't start**: make sure Docker Desktop is running, then `aspire run` again.
- **HTTPS certificate warning**: run `dotnet dev-certs https --trust` once.

## Deploy (optional)
See [docs/deploy.md](docs/deploy.md). Deployment is manual and isn't needed to run or review the code.

## Licence
MIT
