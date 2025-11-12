-- ============================================================================
-- STEP 2: Drop Foreign Key Constraints and Delete Data
-- ============================================================================
-- Drops FK constraints on Ticker column and deletes data from main tables
-- Run this AFTER Step 1 (backup)
-- ============================================================================

BEGIN;

-- Drop foreign key constraints that reference Ticker
-- (These may or may not exist depending on migration history)
DO $$
DECLARE
    fk_name TEXT;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 2: DROP CONSTRAINTS AND DELETE DATA';
    RAISE NOTICE '============================================================================';

    -- Drop FK constraint on transactions if it exists
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_transactions_companies_Ticker'
        AND table_name = 'transactions'
    ) THEN
        ALTER TABLE transactions DROP CONSTRAINT IF EXISTS FK_transactions_companies_Ticker;
        RAISE NOTICE 'Dropped FK constraint: FK_transactions_companies_Ticker';
    ELSE
        RAISE NOTICE 'FK constraint FK_transactions_companies_Ticker not found (may not exist)';
    END IF;

    -- Drop FK constraint on allocation_strategies if it exists
    -- (Check for any FK constraint on Ticker column)
    SELECT tc.constraint_name INTO fk_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
    WHERE tc.table_name = 'allocation_strategies'
    AND kcu.column_name = 'Ticker'
    AND tc.constraint_type = 'FOREIGN KEY'
    LIMIT 1;

    IF fk_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE allocation_strategies DROP CONSTRAINT IF EXISTS %I', fk_name);
        RAISE NOTICE 'Dropped FK constraint: %', fk_name;
    ELSE
        RAISE NOTICE 'No FK constraint found on allocation_strategies.Ticker (may not exist)';
    END IF;
END $$;

-- Delete from transactions
DELETE FROM transactions;

-- Delete from allocation_strategies
DELETE FROM allocation_strategies;

-- Log deletion completion
DO $$
BEGIN
    RAISE NOTICE 'Deleted all data from transactions table';
    RAISE NOTICE 'Deleted all data from allocation_strategies table';
    RAISE NOTICE 'Step 2 Complete: Constraints dropped and data cleared';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'NEXT: Run EF migration (Step 3) before continuing';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

