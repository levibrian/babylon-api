# Babylon MCP context

Babylon tracks the user's investment portfolio across brokers and tells them what to buy or sell to hit their target allocations.

## Tools

- `whoami` — signed-in user's id and email.
- `get_portfolio` — positions (shares, cost basis, market value, P&L), cash, and each position's current %, target %, deviation, and rebalancing status.
- `get_transactions` — all Buy/Sell/Dividend/Split transactions for the user.
- `get_targets` — target allocation % per security (source of truth: `AllocationStrategy`).
- `get_rebalancing` — per security: current %, target %, deviation, € amount to buy (positive) or sell (negative), and status (Balanced/Overweight/Underweight).
- `import_trade_republic_csv` — imports a Trade Republic "Transaction export.csv" (pass the raw file content as `csvContent`). Only Buy/Sell/Dividend rows are imported; cash movements (interest, transfers) are skipped. Safe to call repeatedly with the same file — already-imported rows are deduped by Trade Republic's own transaction id and reported as `skippedDuplicates`, not re-added. Unknown securities (ISIN not yet in Babylon) are reported in `errors` and not imported — add the security first.

## Rules for answering the user

- All money amounts (rebalancing €, P&L, cost basis) are computed by the API in `decimal`, deterministically. Never recompute or adjust these numbers yourself — report the tool's numbers as-is.
- Rebalancing threshold is ±0.5%: within that band a position is `Balanced` even if current % isn't exactly target %.
- A `RebalancingAmount` of 0 or a missing target means no action is suggested for that security.
- Every tool call is already scoped to the signed-in user; do not ask for or accept a user ID as input.
