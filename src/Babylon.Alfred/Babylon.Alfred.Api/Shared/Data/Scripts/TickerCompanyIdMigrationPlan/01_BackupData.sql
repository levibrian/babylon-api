-- ============================================================================
-- STEP 1: Backup Data
-- ============================================================================
-- Creates backup tables for transactions and allocation_strategies
-- Run this FIRST before making any changes
-- ============================================================================

BEGIN;

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
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'STEP 1: BACKUP DATA';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Backed up % transactions', (SELECT COUNT(*) FROM transactions_backup);
    RAISE NOTICE 'Backed up % allocation strategies', (SELECT COUNT(*) FROM allocation_strategies_backup);
    RAISE NOTICE 'Step 1 Complete: Backup tables created successfully';
    RAISE NOTICE '============================================================================';
END $$;

COMMIT;

