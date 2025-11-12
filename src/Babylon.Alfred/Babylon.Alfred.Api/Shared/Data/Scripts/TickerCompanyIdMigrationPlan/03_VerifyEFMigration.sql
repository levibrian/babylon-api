-- ============================================================================
-- STEP 3: Verify EF Migration
-- ============================================================================
-- Verifies that EF Core migration was run successfully
-- Run this AFTER running: dotnet ef database update
-- ============================================================================

BEGIN;

-- Verify EF migration was run (check for CompanyId columns)
DO $$
DECLARE
    has_transactions_company_id BOOLEAN;
    has_allocations_company_id BOOLEAN;
    has_companies_id BOOLEAN;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 3: VERIFY EF MIGRATION';
    RAISE NOTICE '============================================================================';

    -- Check for CompanyId in transactions
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'transactions' AND column_name = 'CompanyId'
    ) INTO has_transactions_company_id;

    -- Check for CompanyId in allocation_strategies
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'allocation_strategies' AND column_name = 'CompanyId'
    ) INTO has_allocations_company_id;

    -- Check for Id in companies
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'companies' AND column_name = 'Id'
    ) INTO has_companies_id;

    -- Verify all required columns exist
    IF NOT has_transactions_company_id THEN
        RAISE EXCEPTION 'Error: CompanyId column not found in transactions table. Please run EF migration first.';
    END IF;

    IF NOT has_allocations_company_id THEN
        RAISE EXCEPTION 'Error: CompanyId column not found in allocation_strategies table. Please run EF migration first.';
    END IF;

    IF NOT has_companies_id THEN
        RAISE EXCEPTION 'Error: Id column not found in companies table. Please run EF migration first.';
    END IF;

    RAISE NOTICE '✓ CompanyId column exists in transactions table';
    RAISE NOTICE '✓ CompanyId column exists in allocation_strategies table';
    RAISE NOTICE '✓ Id column exists in companies table';
    RAISE NOTICE 'Step 3 Complete: EF migration verified - all required columns exist';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

