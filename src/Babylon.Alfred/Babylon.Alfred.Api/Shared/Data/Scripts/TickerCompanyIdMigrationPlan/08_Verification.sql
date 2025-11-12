-- ============================================================================
-- STEP 8: Final Verification
-- ============================================================================
-- Final verification and summary of migration
-- Run this AFTER Step 7 (Make Columns Non-Nullable)
-- ============================================================================

BEGIN;

DO $$
DECLARE
    transaction_count INTEGER;
    allocation_count INTEGER;
    companies_count INTEGER;
    transactions_with_null_company INTEGER;
    allocations_with_null_company INTEGER;
    backup_transaction_count INTEGER;
    backup_allocation_count INTEGER;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 8: FINAL VERIFICATION';
    RAISE NOTICE '============================================================================';

    -- Get counts
    SELECT COUNT(*) INTO transaction_count FROM transactions;
    SELECT COUNT(*) INTO allocation_count FROM allocation_strategies;
    SELECT COUNT(*) INTO companies_count FROM companies;
    SELECT COUNT(*) INTO transactions_with_null_company FROM transactions WHERE "CompanyId" IS NULL;
    SELECT COUNT(*) INTO allocations_with_null_company FROM allocation_strategies WHERE "CompanyId" IS NULL;
    SELECT COUNT(*) INTO backup_transaction_count FROM transactions_backup;
    SELECT COUNT(*) INTO backup_allocation_count FROM allocation_strategies_backup;

    -- Display summary
    RAISE NOTICE '';
    RAISE NOTICE 'MIGRATION SUMMARY';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Companies: %', companies_count;
    RAISE NOTICE 'Transactions: % (Backup had: %, NULL CompanyId: %)', 
        transaction_count, backup_transaction_count, transactions_with_null_company;
    RAISE NOTICE 'Allocation Strategies: % (Backup had: %, NULL CompanyId: %)', 
        allocation_count, backup_allocation_count, allocations_with_null_company;
    RAISE NOTICE '============================================================================';
    RAISE NOTICE '';

    -- Verify no NULL values
    IF transactions_with_null_company > 0 OR allocations_with_null_company > 0 THEN
        RAISE EXCEPTION 'Error: Found NULL CompanyId values. Migration incomplete. Transactions: %, Allocations: %',
            transactions_with_null_company, allocations_with_null_company;
    END IF;

    -- Verify data integrity
    IF transaction_count = 0 AND backup_transaction_count > 0 THEN
        RAISE WARNING 'Warning: No transactions repopulated, but backup had % transactions', backup_transaction_count;
    END IF;

    IF allocation_count = 0 AND backup_allocation_count > 0 THEN
        RAISE WARNING 'Warning: No allocation strategies repopulated, but backup had % strategies', backup_allocation_count;
    END IF;

    RAISE NOTICE '✓ All companies have non-null Id';
    RAISE NOTICE '✓ All transactions have non-null CompanyId';
    RAISE NOTICE '✓ All allocation strategies have non-null CompanyId';
    RAISE NOTICE '';
    RAISE NOTICE 'Migration completed successfully!';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Next steps:';
    RAISE NOTICE '1. Verify application starts successfully';
    RAISE NOTICE '2. Run unit tests';
    RAISE NOTICE '3. Test API endpoints';
    RAISE NOTICE '4. After verification, run Step 9 (Cleanup) to remove backup tables';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

