-- ============================================================================
-- STEP 5: Repopulate Transactions Table
-- ============================================================================
-- Repopulates transactions table with CompanyId from backup
-- Run this AFTER Step 4 (Generate Company IDs)
-- ============================================================================

BEGIN;

-- Insert transactions with CompanyId from backup
INSERT INTO transactions (
    "Id",
    "CompanyId",
    "TransactionType",
    "Date",
    "SharesQuantity",
    "SharePrice",
    "Fees",
    "UserId"
)
SELECT
    t."Id",
    c."Id" AS "CompanyId",
    t."TransactionType",
    t."Date",
    t."SharesQuantity",
    t."SharePrice",
    t."Fees",
    t."UserId"
FROM transactions_backup t
INNER JOIN companies c ON t."Ticker" = c."Ticker";

-- Verify transaction repopulation
DO $$
DECLARE
    original_count INTEGER;
    repopulated_count INTEGER;
    missing_ticker_count INTEGER;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 5: REPOPULATE TRANSACTIONS';
    RAISE NOTICE '============================================================================';

    SELECT COUNT(*) INTO original_count FROM transactions_backup;
    SELECT COUNT(*) INTO repopulated_count FROM transactions;
    SELECT COUNT(*) INTO missing_ticker_count
    FROM transactions_backup t
    WHERE NOT EXISTS (
        SELECT 1 FROM companies c WHERE c."Ticker" = t."Ticker"
    );

    RAISE NOTICE 'Original transactions in backup: %', original_count;
    RAISE NOTICE 'Repopulated transactions: %', repopulated_count;
    RAISE NOTICE 'Missing tickers (not found in companies): %', missing_ticker_count;

    IF missing_ticker_count > 0 THEN
        RAISE WARNING 'Warning: % transactions could not be repopulated (ticker not found in companies)', missing_ticker_count;
        RAISE WARNING 'Review the following tickers:';
        RAISE WARNING '%', (
            SELECT string_agg(DISTINCT t."Ticker", ', ')
            FROM transactions_backup t
            WHERE NOT EXISTS (
                SELECT 1 FROM companies c WHERE c."Ticker" = t."Ticker"
            )
        );
    END IF;

    IF repopulated_count < original_count - missing_ticker_count THEN
        RAISE WARNING 'Warning: Expected to repopulate % transactions, but only % were inserted',
            original_count - missing_ticker_count, repopulated_count;
    END IF;

    RAISE NOTICE 'Step 5 Complete: Transactions repopulated';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

