-- ============================================================================
-- Migration Script: Change from Ticker FK to CompanyId FK
-- ============================================================================
-- This script migrates Transaction and AllocationStrategy tables to use
-- CompanyId instead of Ticker as foreign key.
--
-- Steps:
-- 1. Backup data to temp tables
-- 2. Delete data from main tables
-- 3. (Run EF migration to add CompanyId columns)
-- 4. Generate CompanyId for each company
-- 5. Repopulate tables with CompanyId
-- ============================================================================

BEGIN;

-- ============================================================================
-- STEP 1: Create backup tables and migrate data
-- ============================================================================

-- Backup transactions table
CREATE TABLE IF NOT EXISTS transactions_backup AS
SELECT
    "Id",
    "Ticker",
    "TransactionType",
    "Date",
    "SharesQuantity",
    "SharePrice",
    "Fees",
    "UserId"
FROM transactions;

-- Backup allocation_strategies table
CREATE TABLE IF NOT EXISTS allocation_strategies_backup AS
SELECT
    "Id",
    "UserId",
    "Ticker",
    "TargetPercentage",
    "CreatedAt",
    "UpdatedAt"
FROM allocation_strategies;

-- Log backup completion
DO $$
BEGIN
    RAISE NOTICE 'Step 1 Complete: Backed up % transactions and % allocation strategies',
        (SELECT COUNT(*) FROM transactions_backup),
        (SELECT COUNT(*) FROM allocation_strategies_backup);
END $$;

-- ============================================================================
-- STEP 2: Drop foreign key constraints and delete data from main tables
-- ============================================================================

-- Drop foreign key constraints that reference Ticker
-- (These may or may not exist depending on migration history)
DO $$
DECLARE
    fk_name TEXT;
BEGIN
    -- Drop FK constraint on transactions if it exists
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_transactions_companies_Ticker'
        AND table_name = 'transactions'
    ) THEN
        ALTER TABLE transactions DROP CONSTRAINT IF EXISTS FK_transactions_companies_Ticker;
        RAISE NOTICE 'Dropped FK constraint: FK_transactions_companies_Ticker';
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
    END IF;
END $$;

-- Delete from transactions
DELETE FROM transactions;

-- Delete from allocation_strategies
DELETE FROM allocation_strategies;

-- Log deletion completion
DO $$
BEGIN
    RAISE NOTICE 'Step 2 Complete: Cleared transactions and allocation_strategies tables';
END $$;

-- ============================================================================
-- STEP 3: (Run EF Migration here to add CompanyId columns)
-- ============================================================================
-- Run: dotnet ef database update
-- This will:
--   - Add Id column to companies table
--   - Add CompanyId column to transactions table
--   - Add CompanyId column to allocation_strategies table
--   - Update foreign key constraints

-- Verify EF migration was run (check for CompanyId columns)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'transactions' AND column_name = 'CompanyId'
    ) THEN
        RAISE EXCEPTION 'Error: CompanyId column not found in transactions table. Please run EF migration first.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'allocation_strategies' AND column_name = 'CompanyId'
    ) THEN
        RAISE EXCEPTION 'Error: CompanyId column not found in allocation_strategies table. Please run EF migration first.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'companies' AND column_name = 'Id'
    ) THEN
        RAISE EXCEPTION 'Error: Id column not found in companies table. Please run EF migration first.';
    END IF;

    RAISE NOTICE 'Step 3 Complete: EF migration verified - all required columns exist';
END $$;

-- ============================================================================
-- STEP 4: Generate CompanyId for each company
-- ============================================================================

-- Update companies table: Generate UUID for each company that doesn't have one
UPDATE companies
SET "Id" = gen_random_uuid()
WHERE "Id" IS NULL OR "Id" = '00000000-0000-0000-0000-000000000000'::uuid;

-- Verify all companies have IDs
DO $$
DECLARE
    companies_without_id INTEGER;
BEGIN
    SELECT COUNT(*) INTO companies_without_id
    FROM companies
    WHERE "Id" IS NULL;

    IF companies_without_id > 0 THEN
        RAISE EXCEPTION 'Error: % companies still missing IDs', companies_without_id;
    ELSE
        RAISE NOTICE 'Step 4 Complete: All companies have CompanyId. Total companies: %',
            (SELECT COUNT(*) FROM companies);
    END IF;
END $$;

-- ============================================================================
-- STEP 5: Repopulate transactions table with CompanyId
-- ============================================================================

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
    SELECT COUNT(*) INTO original_count FROM transactions_backup;
    SELECT COUNT(*) INTO repopulated_count FROM transactions;
    SELECT COUNT(*) INTO missing_ticker_count
    FROM transactions_backup t
    WHERE NOT EXISTS (
        SELECT 1 FROM companies c WHERE c."Ticker" = t."Ticker"
    );

    IF missing_ticker_count > 0 THEN
        RAISE WARNING 'Warning: % transactions could not be repopulated (ticker not found in companies)',
            missing_ticker_count;
    END IF;

    RAISE NOTICE 'Step 5a Complete: Repopulated transactions. Original: %, Repopulated: %, Missing: %',
        original_count, repopulated_count, missing_ticker_count;
END $$;

-- ============================================================================
-- STEP 6: Repopulate allocation_strategies table with CompanyId
-- ============================================================================

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
    SELECT COUNT(*) INTO original_count FROM allocation_strategies_backup;
    SELECT COUNT(*) INTO repopulated_count FROM allocation_strategies;
    SELECT COUNT(*) INTO missing_ticker_count
    FROM allocation_strategies_backup a
    WHERE NOT EXISTS (
        SELECT 1 FROM companies c WHERE c."Ticker" = a."Ticker"
    );

    IF missing_ticker_count > 0 THEN
        RAISE WARNING 'Warning: % allocation strategies could not be repopulated (ticker not found in companies)',
            missing_ticker_count;
    END IF;

    RAISE NOTICE 'Step 5b Complete: Repopulated allocation_strategies. Original: %, Repopulated: %, Missing: %',
        original_count, repopulated_count, missing_ticker_count;
END $$;

-- ============================================================================
-- STEP 6: Make CompanyId columns non-nullable (after data is populated)
-- ============================================================================

-- Make CompanyId non-nullable in transactions
ALTER TABLE transactions
    ALTER COLUMN "CompanyId" SET NOT NULL;

-- Make CompanyId non-nullable in allocation_strategies
ALTER TABLE allocation_strategies
    ALTER COLUMN "CompanyId" SET NOT NULL;

-- ============================================================================
-- VERIFICATION: Final checks
-- ============================================================================

DO $$
DECLARE
    transaction_count INTEGER;
    allocation_count INTEGER;
    companies_count INTEGER;
    transactions_with_null_company INTEGER;
    allocations_with_null_company INTEGER;
BEGIN
    SELECT COUNT(*) INTO transaction_count FROM transactions;
    SELECT COUNT(*) INTO allocation_count FROM allocation_strategies;
    SELECT COUNT(*) INTO companies_count FROM companies;
    SELECT COUNT(*) INTO transactions_with_null_company FROM transactions WHERE "CompanyId" IS NULL;
    SELECT COUNT(*) INTO allocations_with_null_company FROM allocation_strategies WHERE "CompanyId" IS NULL;

    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'MIGRATION SUMMARY';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Companies: %', companies_count;
    RAISE NOTICE 'Transactions: % (with NULL CompanyId: %)', transaction_count, transactions_with_null_company;
    RAISE NOTICE 'Allocation Strategies: % (with NULL CompanyId: %)', allocation_count, allocations_with_null_company;
    RAISE NOTICE '============================================================================';

    IF transactions_with_null_company > 0 OR allocations_with_null_company > 0 THEN
        RAISE EXCEPTION 'Error: Found NULL CompanyId values. Migration incomplete.';
    END IF;

    RAISE NOTICE 'Migration completed successfully!';
END $$;

-- ============================================================================
-- OPTIONAL: Cleanup backup tables (uncomment when ready)
-- ============================================================================
-- DROP TABLE IF EXISTS transactions_backup;
-- DROP TABLE IF EXISTS allocation_strategies_backup;

COMMIT;

-- ============================================================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================================================
-- If you need to rollback, run:
--
-- BEGIN;
-- DELETE FROM transactions;
-- DELETE FROM allocation_strategies;
-- INSERT INTO transactions SELECT * FROM transactions_backup;
-- INSERT INTO allocation_strategies SELECT * FROM allocation_strategies_backup;
-- COMMIT;

