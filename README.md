# F# Math Functions Library & Finance API

A comprehensive F# library providing various mathematical functions for different domains,
together with a production-ready ASP.NET Core HTTP API that exposes common finance
calculations over JSON.

## Overview

This repository contains two projects:

| Project | Description |
|---------|-------------|
| `fsMathFunctions` (library) | Pure F# math library (BasicMath, Calculus, Statistics, Geometry, Finance) |
| `FsMathFunctions.Api` (web API) | ASP.NET Core minimal API exposing finance endpoints with API-key auth and Swagger UI |

---

## Finance API

### Rate convention

> **All `annualRatePercent` fields are percent values as you would see them online.**
> A value of `5` means **5 %**, not `0.05`. The API normalises the value internally.

### Running locally with `dotnet run`

```bash
# No API key (development / exploration mode — all /api/* requests are allowed through)
cd FsMathFunctions.Api
dotnet run

# With an API key (recommended for production-like testing)
API_KEY=my-secret-key dotnet run
```

The Swagger UI is always available at <http://localhost:5000/swagger>.

### Setting the API key

Set the `API_KEY` environment variable before starting the server:

```bash
# Linux / macOS
export API_KEY=my-secret-key
dotnet run

# Windows PowerShell
$env:API_KEY = "my-secret-key"
dotnet run
```

Clients must then include the header `X-API-Key: my-secret-key` with every `/api/*` request.

### Example curl requests

```bash
BASE=http://localhost:5000
KEY=my-secret-key   # omit -H "X-API-Key" if no key is configured

# --- Loan payment ---
curl -X POST "$BASE/api/loan/payment" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"principal": 200000, "annualRatePercent": 5, "termYears": 30}'
# → {"monthlyPayment":1073.64,"totalPaid":386510.40,"totalInterest":186510.40}

# --- Mortgage amortization ---
curl -X POST "$BASE/api/mortgage/amortization" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"principal": 300000, "annualRatePercent": 6.5, "termYears": 30,
       "startDate": "2025-01-01", "extraMonthlyPayment": 200}'

# --- Compound interest with monthly contributions ---
curl -X POST "$BASE/api/investment/compound-interest" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"initialPrincipal": 10000, "annualRatePercent": 7, "years": 10,
       "compoundsPerYear": 12, "periodicContribution": 200,
       "contributionFrequency": "monthly"}'
```

### Running with Docker

```bash
# Build the image (run from the repo root)
docker build -t fsmathfunctions-api .

# Run (API key is optional; omit -e API_KEY to disable auth)
docker run -p 8080:8080 -e API_KEY=my-secret-key fsmathfunctions-api

# Test
curl -X POST http://localhost:8080/api/loan/payment \
  -H "Content-Type: application/json" \
  -H "X-API-Key: my-secret-key" \
  -d '{"principal": 200000, "annualRatePercent": 5, "termYears": 30}'
```

### API endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/loan/payment` | Fixed monthly payment, total paid, total interest |
| `POST` | `/api/mortgage/amortization` | Full month-by-month amortisation schedule |
| `POST` | `/api/investment/compound-interest` | Compound interest with optional contributions |

### Authentication

- Header: `X-API-Key: <value>`
- Returns **401** when the header is missing, **403** when the key is wrong.
- Configure the key via the `API_KEY` environment variable.
- The Swagger UI includes an "Authorize" button to enter the key once.

## Building

```bash
# Build everything
dotnet build FsMathFunctions.slnx

# Build library only
dotnet build fsMathFunctions.fsproj

# Build API only
dotnet build FsMathFunctions.Api/FsMathFunctions.Api.fsproj
```

---

## Library modules

### BasicMath

Contains fundamental arithmetic operations like addition, subtraction, multiplication, division, and more.

```fsharp
// Example
open FsMathFunctions.BasicMath

let sum = add 5 3        // 8
let product = multiply 4 2  // 8
```

### Calculus

Provides numerical methods for derivatives and integrals.

```fsharp
// Example
open FsMathFunctions.Calculus

// Calculate derivative of f(x) = x^2 at x = 2 with step h = 0.001
let f x = x * x
let derivative_at_2 = derivative f 2.0 0.001  // Approximately 4

// Calculate integral of f(x) = x^2 from 0 to 1
let integral = integrate f 0.0 1.0 1000  // Approximately 0.333...
```

### Statistics

Offers functions for statistical analysis.

```fsharp
// Example
open FsMathFunctions.Statistics

let data = [1.0; 2.0; 3.0; 4.0; 5.0]
let avg = mean data  // 3.0
let med = median data  // 3.0
let stdDev = standardDeviation data  // Approximately 1.4142
```

### Geometry

Includes functions for geometric calculations.

```fsharp
// Example
open FsMathFunctions.Geometry

let area = circleArea 5.0  // Area of circle with radius 5
let dist = distance (0.0, 0.0) (3.0, 4.0)  // 5.0
```

### Finance

Provides financial calculations for banking and investment.

```fsharp
// Example
open FsMathFunctions.Finance

// Calculate monthly payment for a $200,000 loan at 3.5% interest for 30 years
let payment = monthlyPayment 200000.0 0.035 30  // Approximately $898.09

// Calculate compound interest on $1000 at 5% for 5 years, compounded quarterly
let interest = compoundInterest 1000.0 0.05 5 4  // Approximately $280.08
```

## Installation

Include the library in your F# project by referencing the appropriate assemblies.

## Version

Current version: 1.1.0

## License

[Add your license information here]
