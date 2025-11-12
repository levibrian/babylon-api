-- ============================================================================
-- STEP 7: Make CompanyId Columns Non-Nullable
-- ============================================================================
-- Makes CompanyId columns non-nullable after data is populated
-- Run this AFTER Step 6 (Repopulate Allocation Strategies)
-- ============================================================================

BEGIN;

-- Verify no NULL values exist before making columns non-nullable
DO $$
DECLARE
    transactions_with_null INTEGER;
    allocations_with_null INTEGER;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 7: MAKE COLUMNS NON-NULLABLE';
    RAISE NOTICE '============================================================================';

    SELECT COUNT(*) INTO transactions_with_null FROM transactions WHERE "CompanyId" IS NULL;
    SELECT COUNT(*) INTO allocations_with_null FROM allocation_strategies WHERE "CompanyId" IS NULL;

    IF transactions_with_null > 0 THEN
        RAISE EXCEPTION 'Error: Found % transactions with NULL CompanyId. Cannot make column non-nullable.', transactions_with_null;
    END IF;

    IF allocations_with_null > 0 THEN
        RAISE EXCEPTION 'Error: Found % allocation strategies with NULL CompanyId. Cannot make column non-nullable.', allocations_with_null;
    END IF;

    RAISE NOTICE 'Verified: No NULL CompanyId values found';
END $$;

-- Make CompanyId non-nullable in transactions
ALTER TABLE transactions
    ALTER COLUMN "CompanyId" SET NOT NULL;

-- Make CompanyId non-nullable in allocation_strategies
ALTER TABLE allocation_strategies
    ALTER COLUMN "CompanyId" SET NOT NULL;

DO $$
BEGIN
    RAISE NOTICE 'Made CompanyId non-nullable in transactions table';
    RAISE NOTICE 'Made CompanyId non-nullable in allocation_strategies table';
    RAISE NOTICE 'Step 7 Complete: CompanyId columns are now non-nullable';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

