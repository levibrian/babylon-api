# Babylon Alfred API 🏦

A personal investment portfolio API built with ASP.NET Core. Named after Batman's butler Alfred, it keeps all holdings across brokers in one place and computes FIFO positions, P&L and allocation.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## What it does

- **Transactions**: Buy, Sell, Dividend and Split, single or bulk
- **Securities**: created by ticker via Yahoo Finance search, or manually with full metadata
- **Portfolio**: open positions with FIFO cost basis, market value, unrealized P&L and current allocation
- **Realized P&L**: per sell, FIFO-based
- **Cash** balance tracking
- **History**: hourly portfolio snapshots
- **Auth**: email/password and Google sign-in, JWT + refresh tokens

Rebalancing math exists in `PortfolioCalculator` but is not wired up to target allocations yet.

## Projects

| Project | Purpose |
|---|---|
| `Babylon.Alfred.Api` | REST API, vertical slices under `Features/` |
| `Babylon.Alfred.Worker` | Quartz.NET jobs: price fetching, portfolio snapshots, realized P&L backfill |
| `Babylon.Alfred.Api.Tests` | xUnit tests |

Architecture, data model, testing conventions and per-feature rules live in [`.ai/`](.ai/).

## Getting started

Prerequisites: .NET 9 SDK, PostgreSQL (or Docker).

```bash
# Database
docker run --name babylon-postgres \
  -e POSTGRES_DB=babylon_dev -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 -d postgres:17-alpine

# Point ConnectionStrings:DefaultConnection in appsettings.Development.json at it, then:
cd src/Babylon.Alfred/Babylon.Alfred.Api
dotnet ef database update
dotnet run
```

Or run API + Worker with Docker Compose (API on http://localhost:8000):

```bash
cd src/Babylon.Alfred
docker-compose up
```

Swagger UI is at `/swagger`, health check at `/health`.

## Migrations

```bash
cd src/Babylon.Alfred/Babylon.Alfred.Api
dotnet ef migrations add MigrationName
dotnet ef migrations script     # inspect the SQL
dotnet ef database update
```

Migrations are not applied on startup. See [`.ai/data-model.md`](.ai/data-model.md) for migration rules.

## Tests

```bash
cd src/Babylon.Alfred
dotnet test
```

## Deployment

- **API and Worker**: Fly.io (`fly.api.toml`, `fly.worker.toml`), deployed by GitHub Actions on push to `main`
- **Database**: AWS RDS PostgreSQL, provisioned with Terraform in `iac/components/babylon-api`
- Production config comes from environment variables, e.g. `ConnectionStrings__DefaultConnection`

## Working with Claude Code

Start sessions with `scripts/claude.sh`. It loads only this repo's rules (`CLAUDE.md`, `.claude/`, `.ai/`) and none of your user-level config.
