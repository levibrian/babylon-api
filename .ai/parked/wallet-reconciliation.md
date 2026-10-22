# Self-custody wallet reconciliation

**Status: parked (2026-10-22).** Fully grilled, design settled, not started — paused before build, not rejected. Serves the north star (consolidated holdings view), revisit when ready.

Resume point: design below is final as discussed. Last open thread before parking — how `Drift` status is surfaced/explained isn't an error to auto-fix; it's a signal for the human to investigate and manually log whatever's missing (forgot a buy, airdrop, gift, staking reward). Spec text could still use a short explicit "what Drift means" note before build starts.

## Problem

Crypto holdings (ETH, BTC, SOL) are tracked via the `Transaction` ledger, same model as stocks — FIFO cost basis from imported/manual Buy/Sell/Dividend records. Works cleanly for Trade Republic stocks.

For crypto it's broken down: historical purchases were made on Binance, later moved to Kraken, then to self-custody (currently MetaMask), with untracked transfers, gas fees, and purchases never logged. The ledger no longer reflects reality, and reconstructing exact historical cost basis isn't practical — purchases on Binance/Kraken are off-chain exchange trades; the chain only ever sees the later withdrawal (an address receiving X of an asset, no EUR price attached). Cost basis for exchange-bought crypto can never be recovered from on-chain data alone.

Broader goal: Babylon should be Brian's single consolidated view of wealth, flexible enough to incorporate holdings from multiple external providers (self-custody wallets now; eToro and Revolut later, each their own spec). This spec covers **self-custody wallet reconciliation only** — not tied to any specific wallet software. `ExternalWallet` only needs an address and a chain; it doesn't care whether the key lives in MetaMask, Rabby, a hardware wallet, or anything else. MetaMask is today's concrete instance, not a structural dependency.

Brian's current self-custody holdings span three chains under one MetaMask app: ETH (Ethereum), BTC (Bitcoin), SOL (Solana) — each chain has its own distinct address, so this is three wallets to reconcile, not one.

## Goals

- Verify Babylon's recorded crypto holdings match what's actually on-chain, without requiring precise historical reconstruction.
- Give crypto a clean, accurate cost-basis baseline going forward so P&L and the Trim System (Tom Nash, +50%/+100% triggers) work correctly for crypto again.
- Support future transfers between custody sources (Revolut → self-custody, wallet → wallet) without corrupting cost basis.
- Keep the design extensible to additional wallets/chains later (e.g. a hardware wallet) without rearchitecting.

## Non-goals

- No automated/scheduled syncing (Quartz job, stored balance history). Reconciliation is computed on-demand when portfolio/rebalancing data is requested — no automation needed.
- No support for arbitrary tokens beyond the three in scope (ETH, BTC, SOL) in this spec (extensible later).
- No eToro or Revolut integration (separate specs).
- No attempt to reconstruct historical on-chain or exchange transactions — the migration explicitly resets to a clean baseline (see Migration). On-chain data can confirm current balances and future transfers between Brian's own wallets, but can never supply historical cost basis for exchange-bought crypto (that price only ever existed off-chain).
- No support for dropping cost-basis/P&L tracking for crypto — considered and rejected (see Design decision below); Babylon keeps a thin ledger rather than going balance-only.

## Design

### Design decision: thin forward-only ledger, not full on-chain reconstruction or balance-only

Considered three options:
1. **Balance-only** — drop the ledger entirely, reconcile against on-chain balance only, no cost basis, no P&L for crypto. Rejected: breaks Goal 2 (Trim System / P&L need a cost basis).
2. **Full on-chain reconstruction** — derive cost basis by aggregating historical on-chain transactions, no Babylon-side ledger. Rejected: on-chain data never carries the EUR price paid for exchange purchases (Binance/Kraken trades are off-chain), so this can't actually produce a cost basis for existing holdings.
3. **Thin forward-only ledger (chosen)** — one opening `Buy` transaction per asset today (baseline), then log only new buys/transfers by hand from here, same bookkeeping already required for stocks. No attempt to reconstruct the past.

### Data model

```
ExternalWallet
- Id
- UserId
- Address (string, chain-appropriate format — checksummed for Ethereum, native format for Bitcoin/Solana)
- Chain (enum: Ethereum, Bitcoin, Solana — extensible)
- Label (string, e.g. "MetaMask — ETH", "MetaMask — BTC", "MetaMask — SOL")
- IsActive (bool)
- CreatedAt
```

One row per (address, chain) pair. Brian's current setup: 3 rows (one MetaMask app, three chains, three distinct addresses).

No balance snapshot table. Balances are fetched live on request, not stored or scheduled.

`TransactionType` enum gains a new value: `Transfer`.
- Represents moving holdings between custody sources Brian owns (Revolut → self-custody, wallet → wallet).
- Does not affect cost basis or realized P&L.
- If a network fee was paid in the transferred asset, the fee reduces the oldest FIFO lot's share quantity directly (a quantity shave) — it does **not** flow through the Sell/FIFO-consumption path, so it books zero realized P&L. `TotalAmount`'s switch expression needs a `Transfer` case added (currently falls through to the `Buy`-shaped default, which is wrong — no money moves in a Transfer).

### Components

**Chain balance clients** (`Infrastructure/`) — one per chain, behind a common interface (`IChainBalanceClient.GetBalanceAsync(address) → decimal`):
- **Ethereum** — Etherscan public API (free tier, no key management beyond a public API key).
- **Bitcoin** — Blockstream Esplora API (`blockstream.info/api`), free, no key, same REST/address-balance shape as Etherscan.
- **Solana** — public JSON-RPC (`api.mainnet-beta.solana.com`), `getBalance` method, free but rate-limited; acceptable for v1 given low-frequency on-demand use, revisit if rate-limiting becomes a real problem.

**`WalletReconciliationService`** (`Features/ExternalWallets/`)
- For each active `ExternalWallet` belonging to the user:
  - Fetches on-chain balance via the chain-appropriate client.
  - Computes ledger-derived shares for the matching ticker (reuses existing FIFO logic).
  - Returns: ticker, on-chain balance, ledger balance, drift, status (`Match` / `Drift`).
- Drift tolerance: fixed per-asset constant, not configurable in v1 — chosen small enough to be genuine dust, not a real discrepancy:
  - ETH: 0.0001
  - BTC: 0.00001
  - SOL: 0.001

**Endpoint:** `GET /api/external-wallets/reconciliation`
- Returns reconciliation results for the signed-in user's active wallets.
- Exposed via a new MCP tool, `get_wallet_reconciliation`, and optionally surfaced as an extra field on crypto positions in `get_portfolio`.

### Data flow

Portfolio/rebalancing requests that include crypto positions trigger an on-demand call to `WalletReconciliationService` if the user has any active `ExternalWallet` rows. The reconciliation result is attached to the response.

### Error handling

If a chain client is unavailable or rate-limited, the rest of the portfolio/rebalancing response returns normally. That wallet's reconciliation field is marked `unavailable` with a reason — must never block or degrade the core portfolio response. Each chain client fails independently (an Etherscan outage doesn't block the Bitcoin or Solana result).

### Testing

- Each chain balance client: mocked HTTP tests — normal balance, empty wallet, API error, rate limit.
- `WalletReconciliationService`: unit tests — exact match, drift within tolerance, drift beyond tolerance (per asset, since tolerances differ), ticker with no ledger history at all, one chain client failing while others succeed.
- Integration test: reconciliation endpoint with chain clients mocked.
- `Transfer` transaction type: verify it does not affect cost basis or generate P&L, and correctly shaves the fee off the oldest lot's quantity when present. Verify `TotalAmount` returns 0 for `Transfer` (no money changes hands) even when a fee is set.

## Migration (one-time, manual — not a coded feature)

Operational step Brian performs once per asset (ETH, BTC, SOL), not an automated migration in code:

1. Record the actual current balance for the asset (via the relevant chain explorer or the wallet itself).
2. Delete existing `Transaction` records for that asset — their cost basis no longer reflects reality (Binance → Kraken → self-custody, with untracked transfers and gas fees).
3. Insert one opening `Buy` transaction: quantity = real balance today, price = market price today. This becomes the new cost basis baseline.
4. Going forward:
   - Each Revolut purchase is logged as a normal `Buy` transaction (manually, or via a future Revolut CSV importer — separate spec).
   - Each transfer between Brian's own custody sources (Revolut → self-custody, wallet → wallet) is logged as a `Transfer` — no cost basis impact, only fee if applicable.

Trades away precise historical P&L (impossible to reconstruct cleanly — on-chain data never carries the EUR price paid on an exchange) for a clean, accurate baseline from which the Trim System and crypto P&L tracking work correctly again.

Known side effect, accepted as-is: existing `PortfolioSnapshot` rows and any previously computed `RealizedPnL` predate this baseline reset and will look inconsistent with it going forward. They're not purged — they were accurate for their date, just disconnected from the new baseline. Not a cleanup step; documented here only.

## Open questions / future work

- eToro integration: copy-trading CFD exposure, synthetic — not real custody, needs its own data model (not `Transaction`-based, no traditional cost basis). Separate spec.
- Revolut integration: no public API for retail; likely a CSV importer similar to the existing Trade Republic one. Separate spec.
- Multi-wallet support: `ExternalWallet` already models this (one row per address+chain), but UI/aggregation across multiple wallets for the same ticker isn't built in this spec — add when a second wallet for an already-covered asset (e.g. a hardware wallet also holding ETH) actually gets added.
- Solana public RPC rate limits: no real mitigation planned in v1; revisit (e.g. a keyed provider like Helius) if it becomes a practical problem.
