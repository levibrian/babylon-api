# Import Feature

## Overview

Deterministic CSV import from Trade Republic's "Transaction export.csv". Reuses `TransactionService.CreateBulk` for all money math and FIFO/oversell validation — this feature only parses, resolves securities, and dedupes.

---

## Component Inventory

| Layer | Components |
|-------|-----------|
| Controllers (1) | Import |
| Services (2) | `CsvImportService`, `TradeRepublicCsvParser` (pure, no DI) |

---

## Endpoints

| Route | Methods | Auth |
|-------|---------|------|
| `/api/v1/imports/trade-republic` | POST (multipart file upload) | ✅ |

---

## Trade Republic CSV format

Export columns (relevant subset): `type`, `date`, `symbol` (ISIN, not ticker), `shares`, `price`, `amount`, `fee`, `tax`, `transaction_id`.

- `type` values seen: `BUY`, `SELL`, `DIVIDEND`, `INTEREST_PAYMENT`, `TRANSFER_INBOUND`. Only `BUY`/`SELL`/`DIVIDEND` map to a `Transaction` — the rest are cash movements, out of scope (no cash-import decision made).
- `symbol` is an **ISIN**, resolved via `ISecurityRepository.GetByIsinAsync`. Unknown ISIN → row reported as an error, not imported (no Yahoo-ISIN lookup exists).
- Dividend `amount` is the **gross** amount in EUR; `tax` is the withholding tax (negative in the file). Net amount = `amount - abs(tax)`. The parser passes `TotalAmount` (net) and `Tax` through `CreateTransactionRequest`; `TransactionService` derives gross `SharePrice` via `DividendCalculator` — the parser does not duplicate that math.
- `transaction_id` is a unique UUID per row, used directly as `Transaction.ImportFingerprint` — no hashing needed.

## Dedup

`Transaction.ImportSource` / `ImportFingerprint` (nullable, set only on imported rows). Unique index on `(UserId, ImportFingerprint)` where not null. `CsvImportService` also pre-filters already-imported fingerprints via `ITransactionRepository.GetExistingImportFingerprintsAsync` before building requests, so re-importing the same file adds zero rows and reports them as `SkippedDuplicates`.

## Error handling

Import is all-or-nothing for the *resolved* rows: unknown-ISIN rows are excluded from the batch and reported in `ImportResult.Errors`; the rest go through `CreateBulk` in one call, so a FIFO/oversell violation fails the whole import rather than partially importing (matches `CreateBulk`'s existing contract).
