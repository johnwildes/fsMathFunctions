# FsMathFunctions

A monorepo containing an **F# math library**, a **Finance HTTP API**, and a **multi-user API key management portal** — all deployable to Azure Container Apps.

---

## Architecture

```
┌──────────────────────────────────────┐
│  Portal UI  (React + Fluent UI v9)   │  ← port 3000 locally / public ACA ingress
│  FsMathFunctions.Portal.UI           │
└────────────────┬─────────────────────┘
                 │ JWT-authenticated REST
┌────────────────▼─────────────────────┐
│  Portal Backend  (F# ASP.NET Core)   │  ← port 5001 locally / public ACA ingress
│  FsMathFunctions.Portal              │
│  - POST /auth/register               │
│  - POST /auth/login                  │
│  - GET/POST/DELETE /keys             │
│  - GET/DELETE /admin/users           │
└────────────────┬─────────────────────┘
                 │
        ┌────────▼────────┐
        │   PostgreSQL 16  │  ← port 5432 locally
        └────────┬────────┘
                 │ shared DB
┌────────────────▼─────────────────────┐
│  Finance API  (F# ASP.NET Core)      │  ← port 5000 locally / internal ACA ingress
│  FsMathFunctions.Api                 │
│  - POST /api/loan/payment            │
│  - POST /api/mortgage/amortization   │
│  - POST /api/investment/compound-interest │
│  Validates X-API-Key against         │
│  SHA-256 hashes stored in Postgres   │
└──────────────────────────────────────┘
```

---

## Repository layout

```
fsMathFunctions/
├── fsMathFunctions.fsproj      # Core math library (BasicMath, Calculus, Statistics, Geometry, Finance)
├── FsMathFunctions.Api/        # Finance HTTP API
├── FsMathFunctions.Portal/     # API key management backend (F#)
├── FsMathFunctions.Portal.UI/  # React + Fluent UI v9 frontend
├── FsMathFunctions.Data/       # Shared EF Core entities + DbContext
├── infra/                      # Bicep IaC for Azure Container Apps
├── docker-compose.yml          # Full local stack
├── azure.yaml                  # Azure Developer CLI (azd) config
└── DEPLOYMENT.md               # Detailed Azure deployment steps
```

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | `dotnet --version` |
| [Node.js](https://nodejs.org) | 22+ | For Portal UI |
| [Docker](https://docs.docker.com/get-docker/) | 24+ | For local stack |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | latest | For Azure deploy |
| [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) | latest | Optional — simplifies deploy |

---

## Quick start — deploy to Azure with `azd`

> Provisions all infrastructure (Container Apps, PostgreSQL, ACR, Key Vault) and deploys the three containers in one command.

```bash
# 1. Clone and enter the repo
git clone https://github.com/johnwildes/fsMathFunctions.git
cd fsMathFunctions

# 2. Log in
az login
azd auth login

# 3. Provision + build + deploy
azd up
# Prompts for: environment name, Azure region, dbAdminPassword
```

`azd up` will output the public URLs for the Portal UI and Portal backend.

### Post-deploy: add the JWT secret

```bash
# The Key Vault name is printed in the azd output
az keyvault secret set \
  --vault-name <keyVaultName> \
  --name jwt-secret \
  --value "$(openssl rand -base64 48)"

# Restart the portal so it picks up the secret
az containerapp revision restart \
  --name <prefix>-portal \
  --resource-group <rg>
```

### Promote the first admin account

Register via the Portal UI, then connect to the managed PostgreSQL and run:

```sql
UPDATE users SET role = 'Admin' WHERE email = 'your@email.com';
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for the full step-by-step guide including manual `az` CLI deployment.

---

## Quick start — run locally with Docker Compose

No Azure account needed. Everything runs in containers on your machine.

### 1. Configure secrets

```bash
cp .env.example .env
```

Edit `.env` and set real values for the following variables:

```
JWT_SECRET=change-me-to-a-secret-of-at-least-32-characters
POSTGRES_USER=app
POSTGRES_PASSWORD=change-me
```

`POSTGRES_USER` and `POSTGRES_PASSWORD` are picked up by `docker-compose.yml` at startup (defaults `app` / `devpassword` are used when the variables are not set, but should be overridden for anything beyond a quick local trial).

### 2. Start all services

```bash
docker compose up --build
```

This starts four containers:

| Service | URL | Description |
|---|---|---|
| `postgres` | localhost:5432 | PostgreSQL 16 |
| `finance-api` | http://localhost:5000 | Finance API + Swagger UI |
| `portal` | http://localhost:5001/swagger | Portal backend + Swagger UI |
| `portal-ui` | http://localhost:3000 | React portal |

### 3. Open the portal

Navigate to **http://localhost:3000**, register an account, and start generating API keys.

To promote your account to Admin:

```bash
docker compose exec postgres psql -U postgres -d fsmathdb \
  -c "UPDATE users SET role = 'Admin' WHERE email = 'your@email.com';"
```

### 4. Use a generated API key

```bash
curl -X POST http://localhost:5000/api/loan/payment \
  -H "Content-Type: application/json" \
  -H "X-API-Key: <your-key>" \
  -d '{"principal": 200000, "annualRatePercent": 5, "termYears": 30}'
```

---

## Running services individually (no Docker)

### Finance API

```bash
cd FsMathFunctions.Api
dotnet run
# Swagger UI → http://localhost:5000/swagger
# No DB connection string = key validation is skipped (all requests pass through)
```

### Portal backend

```bash
cd FsMathFunctions.Portal
dotnet run
# Swagger UI → http://localhost:5001/swagger
```

### Portal UI (dev server)

```bash
cd FsMathFunctions.Portal.UI
npm install
VITE_PORTAL_API_URL=http://localhost:5001 npm run dev
# → http://localhost:5173
```

---

## Finance API endpoints

> All `annualRatePercent` values are **percent** (e.g. `5` means 5 %, not 0.05).

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/loan/payment` | Monthly payment, total paid, total interest |
| `POST` | `/api/mortgage/amortization` | Month-by-month amortisation schedule |
| `POST` | `/api/investment/compound-interest` | Compound interest with optional periodic contributions |

Authentication: every `/api/*` request must include `X-API-Key: <key>`. Returns `401` when missing, `403` when invalid or revoked.

### Example requests

```bash
BASE=http://localhost:5000
KEY=<your-api-key>

# Loan payment
curl -X POST "$BASE/api/loan/payment" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"principal": 200000, "annualRatePercent": 5, "termYears": 30}'

# Mortgage amortisation (with extra payment)
curl -X POST "$BASE/api/mortgage/amortization" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"principal": 300000, "annualRatePercent": 6.5, "termYears": 30,
       "startDate": "2025-01-01", "extraMonthlyPayment": 200}'

# Compound interest with monthly contributions
curl -X POST "$BASE/api/investment/compound-interest" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"initialPrincipal": 10000, "annualRatePercent": 7, "years": 10,
       "compoundsPerYear": 12, "periodicContribution": 200,
       "contributionFrequency": "monthly"}'
```

---

## Portal API endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/auth/register` | — | Create a user account |
| `POST` | `/auth/login` | — | Returns a JWT |
| `GET` | `/api/keys` | JWT | List your API keys |
| `POST` | `/api/keys` | JWT | Generate a new key (raw value shown once) |
| `DELETE` | `/api/keys/{id}` | JWT | Revoke a key |
| `GET` | `/admin/users` | JWT (Admin) | List all users |
| `DELETE` | `/admin/users/{id}` | JWT (Admin) | Delete a user and all their keys |

---

## Building

```bash
# All .NET projects
dotnet build FsMathFunctions.slnx

# Portal UI production build
cd FsMathFunctions.Portal.UI && npm run build
```

---

## F# Math Library

The core library (`fsMathFunctions.fsproj`) provides pure-functional math modules.

### BasicMath

```fsharp
open FsMathFunctions.BasicMath

let sum = add 5 3        // 8
```

### Calculus

```fsharp
open FsMathFunctions.Calculus

let f x = x * x
let deriv = derivative f 2.0 0.001   // ≈ 4.0
let integ = integrate f 0.0 1.0 1000 // ≈ 0.333
```

### Statistics

```fsharp
open FsMathFunctions.Statistics

let data = [1.0; 2.0; 3.0; 4.0; 5.0]
let avg = mean data              // 3.0
let med = median data            // 3.0
let sd  = standardDeviation data // ≈ 1.414
```

### Geometry

```fsharp
open FsMathFunctions.Geometry

let area = circleArea 5.0                  // ≈ 78.54
let dist = distance (0.0, 0.0) (3.0, 4.0) // 5.0
```

### Finance

```fsharp
open FsMathFunctions.Finance

let payment  = monthlyPayment 200000.0 0.035 30  // ≈ 898.09
let interest = compoundInterest 1000.0 0.05 5 4  // ≈ 280.08
```

---

## License

[Add your license information here]
