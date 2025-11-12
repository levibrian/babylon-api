-- ============================================================================
-- STEP 9: Cleanup Backup Tables (OPTIONAL)
-- ============================================================================
-- Drops backup tables after successful migration verification
-- ONLY RUN THIS AFTER verifying migration was successful!
-- ============================================================================

BEGIN;

DO $$
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 9: CLEANUP BACKUP TABLES';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'WARNING: This will permanently delete backup tables!';
    RAISE NOTICE 'Only run this after verifying migration was successful.';
    RAISE NOTICE '============================================================================';
END $$;

-- Drop backup tables
DROP TABLE IF EXISTS transactions_backup;
DROP TABLE IF EXISTS allocation_strategies_backup;

DO $$
BEGIN
    RAISE NOTICE 'Dropped transactions_backup table';
    RAISE NOTICE 'Dropped allocation_strategies_backup table';
    RAISE NOTICE 'Step 9 Complete: Backup tables removed';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

