# Babylon revamp

North star and rules: `../decisions.md`.

## Steps

- [ ] **1. Secure the database** (Brian, manual) — _deferred: RDS treated as dev DB; done as part of the Fly migration_
  Rotate RDS password · restrict SG to Fly egress + own IP · connection string to `fly secrets` · repo private.
  _Done when:_ old password rejected, repo private, no secret in `appsettings*.json`.
- [x] **2. Isolation spike**
  `scripts/claude.sh`: `--setting-sources project,local --strict-mcp-config` + `CLAUDE_CODE_DISABLE_AUTO_MEMORY=1`.
  _Verified:_ no user hooks, no work agents/skills, 0 MCP connectors, no auto-memory; project `CLAUDE.md` loads.
  _Residual:_ org-managed plugins still load (company policy); session transcripts still go to `~/.claude/projects/`.
- [x] **3. Feature audit**
  Table in `audit.md`; cuts applied (build green, 261 tests pass).
  Stale context docs fixed in the same PR.
- [x] **3b. CLAUDE.md amendment**
  Rule 0, north star gatekeeper, lightweight plan → approve → build, Angular rules removed.
## Roadmap — first usable MCP from a babylon session

Order: 4a ∥ 4b ∥ 6 → 7 → 7b → 5 → 8. Data is already in the DB, so CSV import is not needed to start.

- [x] **4a. Secure the securities endpoints** — branch `fix/securities-authorize` (merged, #13)
  `[Authorize]` on `SecuritiesController`.
  _Done when:_ unauthenticated calls get 401, with tests.
- [x] **4b. Backend rebalancing** — branch `feat/rebalancing-targets` (`3467e26`, PR pending)
  `AllocationStrategy` repository + target endpoints; wire targets into `PortfolioService` → `PortfolioCalculator` (currently nulls).
  _Done when:_ positions return target %, deviation, status and € buy/sell, with tests.
- [ ] **4c. Rebalancing gaps (found in 4b)**
  Price worker selects securities from `AllocationStrategies` only, so held securities without a target never get prices. Targets on unheld securities produce no "buy" line.
  _Done when:_ worker prices held ∪ targeted securities, and targeted-but-unheld securities appear with a buy amount.
- [x] **6. MCP auth spike** — branch `spike/mcp-oauth` (`3a784e6`, `b77f29f`, PR pending)
  Works locally: `/mcp` + `whoami`, metadata, DCR, PKCE S256, single-use codes, rotating refresh, REST/MCP token separation. Findings: `mcp-auth-spike.md`. Recommendation: keep it in the API.
- [ ] **6b. MCP auth hardening + first connect** — before any real client connects
  Nullable `ClientId` on refresh tokens (manual migration) so REST logins don't revoke MCP sessions and `/auth/refresh` rejects MCP tokens. Confirm `Authentication__Jwt__SecretKey` is a real Fly secret. Deploy, connect Claude Code, call `whoami`.
  _Done when:_ Claude Code calls `whoami` against Fly as Brian, and a web login doesn't disconnect it.
- [ ] **7. MCP tools (read-only first)** — after 4b and 6 merge
  `get_portfolio`, `get_transactions`, `get_targets`, `get_rebalancing` as thin adapters + shared runtime context file. Write tools later.
  _Done when:_ Claude Code and ChatGPT give the same rebalancing answer.
- [ ] **7b. Wire MCP into babylon sessions**
  Repo `.mcp.json` pointing at the Fly `/mcp`; `scripts/claude.sh` adds `--mcp-config .mcp.json` (keeps `--strict-mcp-config`).
  _Done when:_ a `scripts/claude.sh` session lists only the babylon MCP server and can call it.
- [ ] **5. Trade Republic CSV import**
  Deterministic parser, fingerprint dedup, anonymised fixtures.
  _Done when:_ re-importing the same file adds zero rows.
- [ ] **8. Profiling**
  Profile table (markdown, raw answers, rubric version), question guide + rubric in repo, `start_profiling` prompt, `get_profile` / `save_profile`, `set_allocation_targets` with confirmation.
  _Done when:_ a Claude interview ends with a confirmed profile, and ChatGPT reads the same profile back unchanged.

## Open questions
- Does ChatGPT honour MCP server instructions? Fallback: resource / `get_context` tool. (step 7)
- Add Client ID Metadata Documents (spec-preferred; DCR now deprecated but still used by Claude and ChatGPT)? (after 6b)
- Before promoting any rule to structured data: define its inputs, output and precedence vs rebalancing. (after step 8)
