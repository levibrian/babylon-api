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

**Done and verified green in CI** (2026-10-22). PR #20 merged: Unit + Integration/Acceptance jobs both pass on every PR/push to `main`, publishing GitHub Checks via `dorny/test-reporter`. Two follow-up fixes after the first manual Load Test runs surfaced real CI-only issues neither local testing nor PR review caught:

- PR #21 (`fix/load-test-missing-restore`): `load-test.yml` ran `dotnet ef database update` without ever restoring the Api project first — `NETSDK1004`, missing `project.assets.json`. Worked locally only because of a stale local restore from earlier development; a fresh runner has nothing. Fixed by adding `dotnet restore` before the migration step. Verified locally by deleting `obj`/`bin` and reproducing the fresh-checkout state before committing.
- PR #22 (`fix/load-test-k6-network`): `grafana/k6-action` runs k6 inside its own isolated Docker container, so `localhost:8080` inside that container isn't the runner's `localhost` where the Api's port is published — every request failed with `connection refused`. Fixed by installing k6 directly on the runner (apt) and running it as a normal shell step, sharing the runner's network namespace with the published container ports. Not locally verified before commit (k6 isn't installed locally, Brian opted to let CI be the judge) — both PR #21 and #22 are examples of exactly why: issues that only show up on a genuinely fresh runner / real container networking, not reproducible by a developer's already-warmed-up local state.

All three workflows — `CI - Build and Test` (unit + integration jobs) and `Load Test` — are green on `main` as of commit `bdb4617`.

## Next

- Grow the suite organically as features are added — no fixed target count.
- Revisit load test VUs/duration if this ever has more than one real user and throughput starts to matter (currently basic sanity only, per design).
