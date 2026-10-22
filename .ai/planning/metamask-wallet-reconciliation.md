# MetaMask wallet reconciliation

**Status: draft, not started.**

## Problem

Crypto holdings (ETH-EUR, BTC-EUR, SOL-EUR) are tracked via the `Transaction` ledger, same model as stocks — FIFO cost basis from imported/manual Buy/Sell/Dividend records. Works cleanly for Trade Republic stocks.

For crypto it's broken down: historical purchases were made on Binance, later moved to Kraken, then to a self-custody MetaMask wallet, with untracked transfers, gas fees, and purchases never logged. The ledger no longer reflects reality, and reconstructing exact historical cost basis isn't practical.

Broader goal: Babylon should be Brian's single consolidated view of wealth, flexible enough to incorporate holdings from multiple external providers (MetaMask now; eToro and Revolut later, each their own spec). This spec covers **MetaMask only**.

## Goals

- Verify Babylon's recorded crypto holdings match what's actually in the MetaMask wallet, without requiring precise historical reconstruction.
- Give crypto a clean, accurate cost-basis baseline going forward so P&L and the Trim System (Tom Nash, +50%/+100% triggers) work correctly for crypto again.
- Support future transfers between custody sources (Revolut → MetaMask, wallet → wallet) without corrupting cost basis.
- Keep the design extensible to additional wallets later (e.g. a future hardware wallet) without rearchitecting.

## Non-goals

- No automated/scheduled syncing (Quartz job, stored balance history). Reconciliation is computed on-demand when portfolio/rebalancing data is requested — no automation needed.
- No support for arbitrary ERC-20 tokens beyond ETH in this spec (extensible later).
- No eToro or Revolut integration (separate specs).
- No attempt to reconstruct historical Binance/Kraken transactions — the migration explicitly resets to a clean baseline (see Migration).

## Design

### Data model

```
ExternalWallet
- Id
- UserId
- Address (string, checksummed Ethereum address)
- Chain (enum: Ethereum — extensible)
- Label (string, e.g. "MetaMask principal")
- IsActive (bool)
- CreatedAt
```

No balance snapshot table. Balances are fetched live from Etherscan on request, not stored or scheduled.

`TransactionType` enum gains a new value: `Transfer`.
- Represents moving holdings between custody sources Brian owns (Revolut → MetaMask, wallet → wallet).
- Does not affect cost basis or realized P&L.
- If a network fee was paid in the transferred asset, the fee is recorded and subtracted from shares as a small cost — not a disposal for FIFO purposes.

### Components

**`EtherscanClient`** (`Infrastructure/`)
- `GetBalanceAsync(address, ticker) → decimal`
- Calls Etherscan's public API (free tier sufficient for single-wallet, low-frequency use).

**`WalletReconciliationService`** (`Features/ExternalWallets/`)
- For each active `ExternalWallet` belonging to the user:
  - Fetches on-chain balance via `EtherscanClient`.
  - Computes ledger-derived shares for the matching ticker (reuses existing FIFO logic).
  - Returns: ticker, on-chain balance, ledger balance, drift, status (`Match` / `Drift`).
- Drift tolerance: small threshold (e.g. 0.0001 ETH) to absorb dust/rounding — not configurable in v1.

**Endpoint:** `GET /api/external-wallets/reconciliation`
- Returns reconciliation results for the signed-in user's active wallets.
- Exposed via a new MCP tool, `get_wallet_reconciliation`, and optionally surfaced as an extra field on crypto positions in `get_portfolio`.

### Data flow

Portfolio/rebalancing requests that include crypto positions trigger an on-demand call to `WalletReconciliationService` if the user has an active `ExternalWallet`. The reconciliation result is attached to the response.

### Error handling

If Etherscan is unavailable or rate-limited, the rest of the portfolio/rebalancing response returns normally. The reconciliation field is marked `unavailable` with a reason — must never block or degrade the core portfolio response.

### Testing

- `EtherscanClient`: mocked HTTP tests — normal balance, empty wallet, API error, rate limit.
- `WalletReconciliationService`: unit tests — exact match, drift within tolerance, drift beyond tolerance, ticker with no ledger history at all.
- Integration test: reconciliation endpoint with `EtherscanClient` mocked.
- `Transfer` transaction type: verify it does not affect cost basis or generate P&L, and correctly subtracts a fee when present.

## Migration (one-time, manual — not a coded feature)

Operational step Brian performs once, not an automated migration in code:

1. Record the actual current balance in MetaMask for ETH, BTC, SOL (via Etherscan or the wallet itself).
2. Delete existing crypto `Transaction` records (ETH-EUR, BTC-EUR, SOL-EUR) — their cost basis no longer reflects reality (Binance → Kraken → MetaMask, with untracked transfers and gas fees).
3. Insert one opening `Buy` transaction per ticker: quantity = real balance today, price = market price today. This becomes the new cost basis baseline.
4. Going forward:
   - Each Revolut purchase is logged as a normal `Buy` transaction (manually, or via a future Revolut CSV importer — separate spec).
   - Each transfer between Brian's own custody sources (Revolut → MetaMask, wallet → wallet) is logged as a `Transfer` — no cost basis impact, only fee if applicable.

Trades away precise historical P&L (impossible to reconstruct cleanly) for a clean, accurate baseline from which the Trim System and crypto P&L tracking work correctly again.

## Open questions / future work

- eToro integration: copy-trading CFD exposure, synthetic — not real custody, needs its own data model (not `Transaction`-based, no traditional cost basis). Separate spec.
- Revolut integration: no public API for retail; likely a CSV importer similar to the existing Trade Republic one. Separate spec.
- Multi-wallet support: `ExternalWallet` already models this (one row per wallet), but UI/aggregation across multiple wallets for the same ticker isn't built in this spec — add when a second wallet (e.g. hardware wallet) actually gets added.
