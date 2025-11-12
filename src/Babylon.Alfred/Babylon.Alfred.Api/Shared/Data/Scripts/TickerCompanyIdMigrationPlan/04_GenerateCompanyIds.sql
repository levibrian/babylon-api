-- ============================================================================
-- STEP 4: Generate Company IDs
-- ============================================================================
-- Generates UUIDs for all companies that don't have one
-- Run this AFTER Step 3 (EF migration verification)
-- ============================================================================

BEGIN;

-- Update companies table: Generate UUID for each company that doesn't have one
UPDATE companies
SET "Id" = gen_random_uuid()
WHERE "Id" IS NULL OR "Id" = '00000000-0000-0000-0000-000000000000'::uuid;

-- Verify all companies have IDs
DO $$
DECLARE
    companies_without_id INTEGER;
    total_companies INTEGER;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 4: GENERATE COMPANY IDs';
    RAISE NOTICE '============================================================================';

    SELECT COUNT(*) INTO companies_without_id
    FROM companies
    WHERE "Id" IS NULL;

    SELECT COUNT(*) INTO total_companies FROM companies;

    IF companies_without_id > 0 THEN
        RAISE EXCEPTION 'Error: % companies still missing IDs', companies_without_id;
    ELSE
        RAISE NOTICE 'Generated CompanyId for all companies';
        RAISE NOTICE 'Total companies: %', total_companies;
        RAISE NOTICE 'Step 4 Complete: All companies have CompanyId';
        RAISE NOTICE '============================================================================';
    END IF;
END $$;

COMMIT;

