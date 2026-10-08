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
  _Follow-up:_ stale context docs (see `audit.md`).
- [ ] **4. Backend rebalancing**
  Wire `AllocationStrategy` into `PortfolioService` → `PortfolioCalculator.CalculateRebalancingAmount` (currently returns nulls).
  _Done when:_ positions return target %, deviation, status and € buy/sell, with tests.
- [ ] **5. Trade Republic CSV import**
  Deterministic parser, fingerprint dedup, anonymised fixtures.
  _Done when:_ re-importing the same file adds zero rows.
- [ ] **6. MCP auth spike**
  Stub `/mcp` in the API behind OAuth + PKCE + DCR. Decides build-in-API vs hosted provider.
  _Done when:_ claude.ai and ChatGPT both connect and call a stub tool as the right user.
- [ ] **7. MCP tools**
  Thin adapters over services (holdings, transactions, targets, rebalancing) + shared runtime context file.
  _Done when:_ Claude and ChatGPT both get the same rebalancing answer.
- [ ] **8. Profiling**
  Profile table (markdown, raw answers, rubric version), question guide + rubric in repo, `start_profiling` prompt, `get_profile` / `save_profile`, `set_allocation_targets` with confirmation.
  _Done when:_ a Claude interview ends with a confirmed profile, and ChatGPT reads the same profile back unchanged.

## Open questions
- Does ChatGPT honour MCP server instructions? Fallback: resource / `get_context` tool. (step 7)
- Exact OAuth callback URLs and DCR support per client, confirmed against current docs. (step 6)
- Before promoting any rule to structured data: define its inputs, output and precedence vs rebalancing. (after step 8)
