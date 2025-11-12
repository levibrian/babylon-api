-- ============================================================================
-- ROLLBACK SCRIPT
-- ============================================================================
-- Use this script to rollback the migration if something goes wrong
-- Only works if backup tables still exist and EF migration hasn't been fully applied
-- ============================================================================

BEGIN;

DO $$
DECLARE
    backup_transactions_exist BOOLEAN;
    backup_allocations_exist BOOLEAN;
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'ROLLBACK: Restoring data from backup tables';
    RAISE NOTICE '============================================================================';

    -- Check if backup tables exist
    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'transactions_backup'
    ) INTO backup_transactions_exist;

    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'allocation_strategies_backup'
    ) INTO backup_allocations_exist;

    IF NOT backup_transactions_exist THEN
        RAISE EXCEPTION 'Error: transactions_backup table does not exist. Cannot rollback.';
    END IF;

    IF NOT backup_allocations_exist THEN
        RAISE EXCEPTION 'Error: allocation_strategies_backup table does not exist. Cannot rollback.';
    END IF;

    RAISE NOTICE 'Backup tables found. Proceeding with rollback...';
END $$;

-- Clear current data
DELETE FROM transactions;
DELETE FROM allocation_strategies;

RAISE NOTICE 'Cleared current data from transactions and allocation_strategies';

-- Restore transactions (if Ticker column still exists)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'transactions' AND column_name = 'Ticker'
    ) THEN
        INSERT INTO transactions (
            "Id",
            "Ticker",
            "TransactionType",
            "Date",
            "SharesQuantity",
            "SharePrice",
            "Fees",
            "UserId"
        )
        SELECT * FROM transactions_backup;
        
        RAISE NOTICE 'Restored % transactions from backup', (SELECT COUNT(*) FROM transactions);
    ELSE
        RAISE WARNING 'Warning: Ticker column does not exist in transactions table. Cannot restore transactions.';
        RAISE WARNING 'You may need to manually restore data or revert EF migration first.';
    END IF;
END $$;

-- Restore allocation_strategies (if Ticker column still exists)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'allocation_strategies' AND column_name = 'Ticker'
    ) THEN
        INSERT INTO allocation_strategies (
            "Id",
            "UserId",
            "Ticker",
            "TargetPercentage",
            "CreatedAt",
            "UpdatedAt"
        )
        SELECT * FROM allocation_strategies_backup;
        
        RAISE NOTICE 'Restored % allocation strategies from backup', (SELECT COUNT(*) FROM allocation_strategies);
    ELSE
        RAISE WARNING 'Warning: Ticker column does not exist in allocation_strategies table. Cannot restore allocation strategies.';
        RAISE WARNING 'You may need to manually restore data or revert EF migration first.';
    END IF;
END $$;

DO $$
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Rollback completed';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Note: If EF migration was applied, you may need to revert it manually:';
    RAISE NOTICE 'dotnet ef database update <previous_migration_name>';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

