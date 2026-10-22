# Testing infrastructure

Branch: `feat/testing-infrastructure`. Full detail in `.ai/testing.md` (suite taxonomy, how to run) and `.ai/constraints.md` (the config-override gotcha). This file is the decision record.

## Goal

A test framework that grows with the project: Unit (existing), Integration/Acceptance/Functional, and Load — runnable in CI, visible as GitHub Checks, no new persistent infrastructure.

## Decisions

| Area | Decision |
|---|---|
| Suite taxonomy | Combined Integration + Acceptance + Functional into one project (`Babylon.Alfred.Api.IntegrationTests`). For an API-only backend with no UI, splitting them further would be speculative structure, not a real distinction. |
| Database for Integration/Acceptance | Testcontainers Postgres — ephemeral, per-CI-run container, destroyed after. Deliberately **not** EF InMemory for this suite: InMemory silently ignores Postgres-specific constraints (partial unique indexes, cascade deletes) that this suite exists to catch. Deliberately **not** the real RDS dev DB — Brian explicitly didn't want tests touching/costing against a live database. |
| HTTP layer | `WebApplicationFactory<Program>` — real controllers, real middleware, real JSON serialization (Newtonsoft, matching the Api's actual config), not a bypass through services directly. |
| Load test scope | "Basic sanity" (10 VUs/30s against a handful of endpoints), not capacity/stress testing — this is a single-user personal tool today. Revisit if that changes. |
| Load tool | k6 — scriptable, has a maintained GitHub Action, doesn't need to live inside the .NET solution. |
| Load target | Real Api Docker image + plain Postgres, both spun up fresh in the CI runner via `load/docker-compose.load.yml`, torn down after. Not the deployed Fly apps. |
| Triggers | Unit + Integration/Acceptance/Functional: same as today (push/PR to `main`), two parallel jobs. Load: `workflow_dispatch` only — slower, spins up real containers, no reason to run it on every push. |
| Results visibility | `dorny/test-reporter` publishes `.trx` results as GitHub Checks on both CI jobs — pass/fail/drill-down visible directly on the PR/Actions run, no external dashboard. Load test writes a summary to the run's Job Summary + uploads the k6 JSON as an artifact. |
| Branching | All of this work on `feat/testing-infrastructure`, not `main` directly — Brian wants to review via PR. |

## Incident (2026-10-21)

First Integration/Acceptance test run wrote 2 real users to the live RDS dev database. Root cause: `Program.cs` reads `ConnectionStrings:DefaultConnection` and `Authentication:Jwt:SecretKey` directly off `builder.Configuration` *before* `builder.Build()` — `WebApplicationFactory.ConfigureWebHost(...).ConfigureAppConfiguration(...)` only takes effect *during* `Build()`, too late for those specific reads, so they silently fell through to the real `appsettings.Development.json` values.

Caught immediately via a direct DB query (not by the test itself — it passed, incorrectly, against the real DB). Stray rows deleted and verified removed. Fixed by setting real process environment variables before the factory's `Services` is first touched, instead of relying on `ConfigureAppConfiguration` — see `BabylonApiFactory.cs` and the `constraints.md` entry.

## Status

Scaffolded and passing locally (Docker started manually for local verification): 6 tests across 3 acceptance flows (auth, transaction→portfolio, CSV import dedup) and 2 repository-level integration tests (real unique-index enforcement, fingerprint lookup). CI wiring and load test workflow written but not yet run in CI — first real CI run happens once this branch is pushed.

## Next

- Push `feat/testing-infrastructure`, open a PR, let CI run both new jobs for real.
- Run the load workflow manually once via the Actions tab to confirm `grafana/k6-action` + the compose stack work end-to-end in CI (not locally verified — k6 isn't installed on this machine).
- Grow the suite organically as features are added — no fixed target count.
