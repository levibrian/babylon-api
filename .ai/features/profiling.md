# Profiling Feature (Design — step 8 of `../planning/revamp.md`)

> Status: **Design approved, not implemented.** This doc is the spec for `revamp.md`'s step 8.
> Decisions referenced here are locked in `../decisions.md` (lines 27-34, 59) — not re-opened by this doc.

## Overview

Stores the user's investor profile (risk tolerance, investment facts, and personal investment philosophy) as a single markdown document in the database, built through an LLM-led MCP interview instead of a fixed questionnaire. Replaces `financial.md`-in-the-vault as the source of truth: any agent (Claude, ChatGPT) reads the same profile via `get_profile` before giving investment advice.

Scoped to Brian for now (single real user), but the schema is per-user from day one — no onboarding/multi-tenant work needed later.

---

## Component Inventory

| Layer | Components |
|-------|-----------|
| Model (1) | `Profile` |
| Repository (1) | `IProfileRepository` / `ProfileRepository` |
| Service (1) | `IProfileService` / `ProfileService` (new `Features/Profiling/` slice) |
| MCP Tools (4) | `get_profiling_guide`, `get_profile`, `save_profile` (`ProfileTool.cs`, new) · `set_allocation_targets` (added to existing `TargetsTool.cs`) |
| Repo content (1) | `.ai/profiling-guide.md` (new — question guide + rubric, not code) |

No REST controller: MCP tools call `Babylon.Alfred.Api` services in-process (Worker pattern, confirmed in `decisions.md` → "MCP hosting"/"Adapters"). No frontend work — MCP-only for now.

---

## Data model

```csharp
// Shared/Data/Models/Profile.cs
public class Profile
{
    public Guid UserId { get; set; }              // PK + FK -> User, 1:1
    public string? ProfileMarkdown { get; set; }   // fixed section headings, see below
    public string? RawAnswers { get; set; }        // interview transcript, markdown
    public string? RubricVersion { get; set; }     // e.g. "v1"
    public DateTime UpdatedAt { get; set; }        // UTC, set on every upsert
    public User User { get; set; } = null!;
}
```

- PK = `UserId` (no separate `Id` — strict 1:1 with `User`, no history table; `decisions.md` rules out a version history for now, not re-opened here)
- All three content fields nullable until the first `save_profile` call
- EF config: `Shared/Data/Configurations/ProfileConfiguration.cs`, new migration `AddProfile`
- Column type: `text` (unbounded), not `varchar(n)`

### `ProfileMarkdown` fixed section headings

```markdown
## Risk Tolerance
## Investment Horizon
## Runway
## Contribution Capacity
## Tax Residence
## Hard Exclusions
## Investment Philosophy & Rules
```

The first six sections are produced by the interview (see below). **Investment Philosophy & Rules** is seeded once from Brian's existing `financial.md` content (Tom Nash framework, DCAX2, Trim System, values) almost verbatim — it does not go through the interview/rubric process, since Brian already wrote and owns it. Future edits to this section happen the same way any `save_profile` call works: LLM proposes the full updated markdown, Brian confirms, tool call overwrites.

---

## `.ai/profiling-guide.md` (repo content, not DB)

Shared by all users, versioned with the API (per `decisions.md` → "Question guide + rubric"). Copied into the Mcp project's output via the same `<None Include>` csproj mechanism already used for `mcp-context.md`, but **not** folded into `ServerInstructions` — it's large and only needed when actively profiling, so it's served on-demand via the `get_profiling_guide` tool instead of loaded into every session's context.

### Inferred dimension — Risk Tolerance

Closed multiple-choice, no finance language, no open narrative (fast to answer — this was explicitly corrected during design to avoid open-ended "tell me about a time..." prompts that invite long answers):

| # | Question | A | B | C |
|---|---|---|---|---|
| 1 | Planeás un viaje | Reservo todo con semanas de anticipación | Reservo lo esencial, el resto lo decido ahí | Agarro la mochila y veo qué sale |
| 2 | Se cancela un plan importante de último momento | Me frustro bastante, tardo en re-planear | Lo acomodo y sigo el día | Ya tenía plan B antes de que termine de cancelarse |
| 3 | Esperás un resultado importante (laburo/trámite) | Reviso el mail cada rato | Reviso un par de veces por día | Me desconecto, me entero cuando me entero |
| 4 | Decisión grande de vida (mudanza, cambio de carrera) | Necesito casi todo mapeado antes de mover un dedo | Junto info razonable y decido | Decido rápido con lo que tengo, ajusto después |
| 5 | Actividades con riesgo físico (motos, buceo, alturas) | Las evito activamente | Las hago alguna vez si se da | Las busco activamente |
| 6 | Jugando con amigos (sin plata real en juego), vas perdiendo | Me bajo/paro | Sigo como estaba | Subo la apuesta pa recuperar |

**Rubric:** A ≈ Conservative weight, B ≈ Moderate weight, C ≈ Aggressive weight. The LLM reads the pattern across all 6 answers and writes the dominant tendency into `## Risk Tolerance` as prose (Conservative / Moderate / Aggressive / Mixed), not a numeric score. A mixed/split pattern is reported as "mixed" in the confirmation step rather than forced into one bucket — matches `decisions.md`'s rejection of finance-quiz-style scoring (people misjudge themselves when asked directly; behavior-based signal is more honest, but it's still a qualitative read, not an algorithm).

### Explicit dimensions — plain fact questions (no inference)

| Question | Section |
|---|---|
| "¿Para cuándo pensás necesitar esta plata — o no tiene fecha?" | Investment Horizon |
| "Si dejaras de tener ingresos hoy, ¿cuántos meses podés cubrir gastos sin vender nada?" | Runway |
| "¿Cuánto podés aportar por mes/año, más o menos?" | Contribution Capacity |
| "¿Dónde pagás impuestos — país de residencia fiscal?" | Tax Residence |
| "¿Hay algo que NUNCA quieras tener en tu portfolio (sector, empresa, tipo de activo)?" | Hard Exclusions |

`RubricVersion = "v1"`. Bump manually whenever the guide's questions or interpretation rules change meaningfully; `RawAnswers` is kept specifically so profiles can be re-derived later if the rubric changes (per `decisions.md` → "Shared rubric").

---

## End-to-end workflow

1. Brian (or the LLM proactively) starts profiling.
2. LLM calls `get_profiling_guide` → gets the question guide + rubric above.
3. LLM asks the 6 risk-tolerance questions + 5 fact questions. For Brian specifically, answers already present in his current `financial.md` are not re-asked — only gaps/ambiguities are.
4. LLM synthesizes the full `ProfileMarkdown` (all 7 sections, "Investment Philosophy & Rules" carried over from `financial.md` for the initial migration).
5. LLM shows the complete markdown to the user. **Nothing is saved until the user confirms or corrects it** (per `decisions.md` → "Profile confirmation").
6. LLM calls `save_profile(profileMarkdown, rawAnswers, rubricVersion)`.
7. Any future session (this one or another agent) calls `get_profile` before giving investment advice, instead of reading the vault's `financial.md`.
8. If the interview surfaces target-allocation suggestions, they are **not** written automatically. The LLM proposes them in conversation; only after explicit confirmation does it call `set_allocation_targets` (per `decisions.md` → "Target guardrail"). This is a separate confirmation from step 5 — confirming the profile does not imply confirming targets.

---

## MCP Tools

### `get_profiling_guide`
- Read-only. No input (no user scoping needed — guide is global, not per-user).
- Reads `profiling-guide.md` from `AppContext.BaseDirectory` (same pattern as `mcp-context.md` loading in `ServiceCollectionExtensions`), returns raw markdown string.
- If the file is missing at runtime (deploy error), throws — no silent fallback. Unlike `mcp-context.md` (where a missing file just means no `ServerInstructions`, non-fatal), profiling cannot proceed without the guide, so failing loudly is correct here.

### `get_profile`
- Read-only. Scoped to signed-in user (`ClaimsPrincipal` + `GetUserId()`, same pattern as `WhoAmITool`/`PortfolioTool`).
- Returns `ProfileDto { ProfileMarkdown, RawAnswers, RubricVersion, UpdatedAt }`, all nullable.
- No profile saved yet → all fields `null`, HTTP-equivalent 200 (not an error state).

### `save_profile`
- Write, idempotent (re-saving the same content is a no-op in effect — same upsert pattern as `import_trade_republic_csv`).
- Input: `profileMarkdown` (string, required), `rawAnswers` (string, required), `rubricVersion` (string, required).
- Validation: any of the three empty/null → throws `ArgumentException`. No length limit.
- Upserts the single `Profile` row for the signed-in user; sets `UpdatedAt = DateTime.UtcNow`.

### `set_allocation_targets` (added to existing `TargetsTool.cs`)
- Write, idempotent — thin wrapper over `IAllocationService.ReplaceTargets(userId, request)`, which **already exists** and is already used by `AllocationsController.Put` today. Zero new business logic.
- Input: list of `{ ticker, targetPercentage }`.
- `Description` attribute explicitly instructs the LLM: call this only after the user has explicitly confirmed the targets in conversation.
- All validation (unknown ticker, percentage range, sum-to-100 or whatever `AllocationService` already enforces) is inherited as-is — not re-specified here.

---

## Error handling

| Case | Behavior |
|---|---|
| `get_profile`, no profile saved yet | 200-equivalent, all fields `null` — valid state, not an error |
| `save_profile`, empty/null `profileMarkdown` or `rubricVersion` | `ArgumentException` |
| `set_allocation_targets`, invalid ticker/percentage | Delegated entirely to `IAllocationService.ReplaceTargets`'s existing validation |
| `get_profiling_guide`, file missing on disk | Throws — fatal, no fallback (profiling can't proceed without it) |
| Any tool, unauthenticated | `UnauthorizedAccessException`, same as every existing tool |

---

## Testing

- `ProfileRepositoryTests` — upsert (insert when absent, update when present), `GetByUserIdAsync` returns null when no row exists
- `ProfileServiceTests` (Moq'd repository) — validation of required fields before persisting
- `Mcp/ProfileToolTests` — `get_profile`, `save_profile` happy paths + validation errors; `get_profiling_guide` happy path + missing-file case; unauthenticated → exception for all three
- `Mcp/TargetsToolTests` — add case for `set_allocation_targets` calling `IAllocationService.ReplaceTargets` with the expected arguments
- No integration tests (no `WebApplicationFactory`/`TestServer` infra exists anywhere in this repo today — adding it would be new cross-cutting infra, out of scope for this feature)

---

## Out of scope / risks

- **No version history** of the profile — only the current value is kept (`RawAnswers` lets it be re-derived if the rubric changes, but intermediate states are not stored). Revisit only if a real need shows up.
- **No frontend UI** — MCP-only. Revisit if/when Babylon is offered to other users.
- **Regulatory note** (already flagged in `ideas.md`): profiling that leads to a recommended strategy is a MiFID II suitability assessment — i.e. regulated financial advice — **once offered to other users**. Fine for Brian's personal use; must be revisited before `ideas.md`'s "publish Babylon as a product" idea proceeds.
- **Migration of Brian's current `financial.md`**: in scope for this feature (per earlier decision) — done by running the workflow above once, not by a data migration script. No EF seed needed.

---

## Open questions

- None blocking. `RubricVersion` bump policy ("meaningfully changed") is a judgment call each time, not automated — acceptable per `decisions.md`'s Rule 0.
