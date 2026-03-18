# BrokerIntegration Feature — CLAUDE.md

## Purpose
Manages broker account connections and transaction synchronization for Babylon.
Currently supports: Trade Republic (TradeRepublic).

## API Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST | /api/v1/broker/pair/initiate | Start TR device pairing (phone + PIN → sends SMS code to user) |
| POST | /api/v1/broker/pair/complete | Complete pairing with SMS code → stores encrypted session |
| POST | /api/v1/broker/sync | Trigger a sync (FullBackfill on first pair, DeltaSync thereafter) |
| GET  | /api/v1/broker/status | Current connection status for the authenticated user |
| GET  | /api/v1/broker/sync/{jobId} | Poll a specific sync job's state and counters |
| DELETE | /api/v1/broker/revoke | Revoke the broker connection (sets Status=Revoked) |

## Transaction States After Reconciliation
| Source | SyncStatus | Meaning |
|--------|-----------|---------|
| Manual | null | User-entered, never touched by broker sync |
| BrokerSynced | Reconciled | Imported from broker, no prior manual match existed |
| BrokerVerified | Reconciled | Was Manual, broker confirmed it (exact or fingerprint match) |
| BrokerVerified | Conflicted | Was Manual, broker matched on Ticker/Date/Qty but price differed >0.5% — broker price applied |

## Business Rules (INVARIANTS — test every one of these)
1. Phone number and PIN are NEVER stored, logged, or persisted. Only `TrDeviceCredentials` (device_id + device_key) are stored.
2. Stored credentials are encrypted with AES-256-GCM. Encryption key (`BROKER_ENCRYPTION_KEY_BASE64`) lives in environment variables only — never in DB.
3. When the TR session expires, `BrokerConnection.Status` is set to `Expired` and the API returns `HTTP 401` with `{ code: "BrokerSessionExpired" }`. The user must re-pair.
4. Reconciliation strategy order is strict: ExactId → Fingerprint → Fuzzy. First match wins.
5. Fingerprint = SHA-256(Date.Date (yyyy-MM-dd) + "|" + Ticker.ToUpper() + "|" + SharesQuantity.ToString("F8") + "|" + TransactionType.ToString())
6. Fuzzy match criteria (ALL must hold): Ticker exact + SharesQuantity exact + TransactionType exact + |ΔDate| ≤ 24h
7. Broker always wins on price conflict (|ΔPrice%| > 0.5%) — local price is overwritten silently.
8. Before the first production FullBackfill, a manual `pg_dump` must be taken by the operator (see Phase 5.5). This is a deployment checklist item, not automated code.
9. No concurrent sync jobs per user. If a job is in progress (any state except Completed/Failed), StartSync throws `InvalidOperationException`.
10. All broker-originated transactions are fingerprinted on insert. The `Fingerprint` column has a partial unique index (`WHERE "Fingerprint" IS NOT NULL`) preventing duplicate imports.
11. Outbox events and transaction writes are committed in a single DB transaction (Persisting step). This guarantees at-least-once event delivery.
12. Manual transactions (`Source = Manual`) that match incoming broker transactions are promoted to `BrokerVerified` — they are NOT deleted and re-inserted.

## Saga States
Pending → Authenticating → FetchingHistory → Reconciling → Persisting → Completed
Any step failure → Failed (ErrorMessage recorded on BrokerSyncJob)

## Encryption Details
- Algorithm: AES-256-GCM (System.Security.Cryptography.AesGcm)
- Key size: 256-bit (32 bytes), stored Base64-encoded
- Nonce: 12 bytes, random per encryption call, stored in BrokerConnection.Nonce
- Tag: 16-byte GCM authentication tag, stored in BrokerConnection.Tag
- Ciphertext: Base64 in BrokerConnection.EncryptedSessionData
- AesGcmEncryptionService registered as Singleton (key loaded once at startup)

## Outbox Event Types
| EventType | AggregateType | When |
|-----------|--------------|------|
| TransactionImported | Transaction | New broker tx with no prior manual match |
| TransactionConflictResolved | Transaction | Fuzzy match, broker price overwrote manual price |
| BrokerSessionExpired | BrokerConnection | TR ping fails with auth error |
| SyncCompleted | BrokerSyncJob | Saga reaches Completed |
| SyncFailed | BrokerSyncJob | Saga reaches Failed |

## DO NOT Rules
- DO NOT store phone number, PIN, or any raw TR login credentials anywhere
- DO NOT allow concurrent sync jobs for the same user
- DO NOT run the first FullBackfill in production without first taking a manual `pg_dump` (see Phase 5.5 checklist)
- DO NOT write outbox events outside the Persisting DB transaction
- DO NOT implement new matching strategies without extending all three strategy tests
- DO NOT add new broker types without implementing ITradeRepublicClient equivalent

## DI Registration
All services registered via `Features/BrokerIntegration/Extensions/ServiceCollectionExtensions.cs::RegisterBrokerIntegration()`.
AesGcmEncryptionService → Singleton.
All repositories, services, orchestrators → Scoped.
ITradeRepublicClient registered with AddHttpClient<> pattern (or WebSocket factory).

## Worker Jobs
- BrokerDeltaSyncJob: Cron `0 0 * ? * *` (every hour) — delta sync for all users with Active connections
- OutboxProcessorJob: Cron `0/30 * * ? * *` (every 30s) — process pending outbox events, max 5 retries
