-- ============================================================================
-- STEP 6: Repopulate Allocation Strategies Table
-- ============================================================================
-- Repopulates allocation_strategies table with CompanyId from backup
-- Run this AFTER Step 5 (Repopulate Transactions)
-- ============================================================================

BEGIN;

-- Insert allocation_strategies with CompanyId from backup
INSERT INTO allocation_strategies (
    "Id",
    "UserId",
    "CompanyId",
    "TargetPercentage",
    "CreatedAt",
    "UpdatedAt"
)
SELECT
    a."Id",
    a."UserId",
    c."Id" AS "CompanyId",
    a."TargetPercentage",
    a."CreatedAt",
    a."UpdatedAt"
FROM allocation_strategies_backup a
INNER JOIN companies c ON a."Ticker" = c."Ticker";

-- Verify allocation_strategies repopulation
DO $$
DECLARE
    original_count INTEGER;
    repopulated_count INTEGER;
    missing_ticker_count INTEGER;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 6: REPOPULATE ALLOCATION STRATEGIES';
    RAISE NOTICE '============================================================================';

    SELECT COUNT(*) INTO original_count FROM allocation_strategies_backup;
    SELECT COUNT(*) INTO repopulated_count FROM allocation_strategies;
    SELECT COUNT(*) INTO missing_ticker_count
    FROM allocation_strategies_backup a
    WHERE NOT EXISTS (
        SELECT 1 FROM companies c WHERE c."Ticker" = a."Ticker"
    );

    RAISE NOTICE 'Original allocation strategies in backup: %', original_count;
    RAISE NOTICE 'Repopulated allocation strategies: %', repopulated_count;
    RAISE NOTICE 'Missing tickers (not found in companies): %', missing_ticker_count;

    IF missing_ticker_count > 0 THEN
        RAISE WARNING 'Warning: % allocation strategies could not be repopulated (ticker not found in companies)', missing_ticker_count;
        RAISE WARNING 'Review the following tickers:';
        RAISE WARNING '%', (
            SELECT string_agg(DISTINCT a."Ticker", ', ')
            FROM allocation_strategies_backup a
            WHERE NOT EXISTS (
                SELECT 1 FROM companies c WHERE c."Ticker" = a."Ticker"
            )
        );
    END IF;

    IF repopulated_count < original_count - missing_ticker_count THEN
        RAISE WARNING 'Warning: Expected to repopulate % allocation strategies, but only % were inserted',
            original_count - missing_ticker_count, repopulated_count;
    END IF;

    RAISE NOTICE 'Step 6 Complete: Allocation strategies repopulated';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

