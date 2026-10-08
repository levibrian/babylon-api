# Decisions

## Rule 0 — Keep it simple. Always.
Overrides everything else. If a simpler option works, take it.

## North star
> One place for all my holdings across brokers that tells me exactly what to buy and sell to hit my target allocations.

Every feature must serve this sentence. If it doesn't, it goes to `ideas.md`.

## Decisions

| Area | Decision |
|---|---|
| Agent | Amend `CLAUDE.md` in this repo. Babylon-only. Short plan → approval → build. Pushes back once on off-north-star ideas, then defers. |
| Isolation | Start sessions with `scripts/claude.sh` (project/local settings only, no MCP connectors, no auto-memory). Nothing in `~/.claude`. |
| Planning | One markdown file per initiative in `.ai/planning/`, plus `decisions.md` and `ideas.md`. |
| Repo | Private. |
| Targets | `AllocationStrategy.TargetPercentage` is the single source of truth. Recurring schedules are the contribution plan only. |
| Rebalancing | € buy/sell per position, computed in the backend (`decimal`, deterministic). LLMs never do the math. |
| Audit | One bounded pass: keep/cut/park table vs the north star. Not a revamp. |
| Code | Refactor in place, never rewrite. FIFO calculators are the trusted base. |
| Adapters | REST controllers and MCP tools are thin; all logic lives in services. No frontend-specific shapes. |
| CSV import | Trade Republic first. Deterministic parser, fingerprint dedup, anonymised fixtures only. |
| MCP context | One runtime context file in the repo, served by the MCP server to Claude and ChatGPT alike. |
| Database | AWS RDS = development database for now. Hardening (secrets, network, encryption at rest) happens as part of the Fly Postgres migration. |
| Sequence | Isolation spike → audit → backend rebalancing → TR CSV → MCP auth spike → MCP tools → profiling. DB hardening rides with the Fly migration. |
| Per-user | Every query and MCP tool is scoped by `UserId`. No onboarding/billing for other users yet. |
| User profile | Stored in the database, one row per user: profile markdown (fixed section headings), raw interview answers, rubric version, `updated_at`. Served via MCP; the LLM reasons over it. |
| Question guide + rubric | Shared by all users, so it lives in the repo (versioned, reviewed, deployed with the API), not in the database. |
| Profiling | LLM-led interview via MCP prompt `start_profiling` → `save_profile`. Mostly everyday-life questions; personality is inferred, never asked in finance terms. Plus a few plain fact questions for what can't be inferred: horizon, months of runway without income, contributions, tax residence, hard exclusions. No jargon. |
| Profile confirmation | The interview ends by showing the user its interpretation; nothing is saved until they confirm or correct it. |
| Shared rubric | The guide defines the profile dimensions and how answers map to them, so Claude and ChatGPT read the same answers the same way. Raw answers are kept so profiles can be re-derived when the rubric changes. |
| Target guardrail | The interview may suggest targets; they reach `AllocationStrategy` only via `set_allocation_targets` after explicit user confirmation. |
| Actionable rules | Any rule that outputs an amount or buy/sell moves into structured data computed by the API, once acted on with real money. |
| MCP hosting | MCP endpoint inside the API (`/mcp`, official C# SDK). One hop, one auth layer, one deploy. |
| MCP auth | OAuth (authorization code + PKCE) only, login via existing Google sign-in. Dynamic Client Registration; strict redirect URI allowlist. Works in Claude Code, claude.ai, Desktop and ChatGPT. |
| Auth server | Minimal authorization server inside the API; fall back to a hosted DCR-capable provider if the spike shows it's more than a few days of work. |

## Rejected alternatives

| Alternative | Why not |
|---|---|
| Separate personal-planning repo | One project; more to maintain. |
| plan + stories + HTML (work-style) | Three files drift without enforcement tooling. |
| `feature.html` | Noisy diffs, needs a template. Markdown renders everywhere. |
| Targets derived from recurring amounts | Targets shift silently when contributions pause or change. |
| LLM maps the CSV columns | Non-deterministic money data. |
| MCP first | Would expose a rebalancing endpoint that returns nulls. |
| Full rewrite / revamp | That's how the previous redesign died. |
| Logging every override | Brian's call: defer without a record. |
| Plan-only agent | Two agents, two configs, double the drift. |
| User-level `CLAUDE_CONFIG_DIR` alias | Brian wants nothing stored outside the repo. |
| Rules applied by the LLM from prose | Claude and ChatGPT would compute different sell amounts. |
| Separate MCP service calling the API | Two deploys and token forwarding for no gain. |
| Finance-quiz questions ("bonds or ETFs?") | People misjudge their own risk appetite; behaviour is more honest. |
| Inferring horizon, runway and tax from personality | Willingness to take risk is not capacity for it; guessing these is guessing about money. |
| Profile as a file in the API | Per-user data belongs in the database. |
| API-driven fixed questionnaire | More code (questions, answers, flow) and can't adapt to answers. |
| Pre-registered OAuth client IDs | Manual setup per client and per user. |
| Personal access token with manual refresh | Web connectors don't accept custom headers; would be thrown away once OAuth lands. |

## Known risks
- Work Claude account sees personal portfolio data.
- No override log: drift is only visible from memory.
- Yahoo Finance v8 is unofficial; prices can silently go stale.
- AWS RDS is treated as a development database (holding real data): public password, open security group and no encryption at rest are accepted until the Fly migration.
