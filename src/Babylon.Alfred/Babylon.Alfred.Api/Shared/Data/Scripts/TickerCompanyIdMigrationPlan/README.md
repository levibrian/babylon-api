# Ticker → CompanyId Migration Plan

This folder contains step-by-step SQL scripts to migrate from using `Ticker` as a foreign key to using `CompanyId` (Guid) as the foreign key.

## Overview

The migration changes:
- **Transaction** table: `Ticker` (string FK) → `CompanyId` (Guid FK)
- **AllocationStrategy** table: `Ticker` (string FK) → `CompanyId` (Guid FK)
- **Company** table: Primary key changes from `Ticker` to `Id` (Guid)

## Prerequisites

1. **Backup your database** before starting:
   ```bash
   pg_dump -U postgres -d babylon_db > backup_before_migration_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Stop the application** or put it in maintenance mode

3. **Ensure you have database admin access**

## Execution Order

### Step 1: Backup Data
```bash
psql -U postgres -d babylon_db -f 01_BackupData.sql
```
Creates backup tables: `transactions_backup` and `allocation_strategies_backup`

### Step 2: Drop Constraints and Delete Data
```bash
psql -U postgres -d babylon_db -f 02_DropConstraintsAndDeleteData.sql
```
Drops foreign key constraints and clears data from main tables

### Step 3: Run EF Core Migration
```bash
cd src/Babylon.Alfred/Babylon.Alfred.Api
dotnet ef migrations add ChangeToCompanyIdForeignKey --context BabylonDbContext
dotnet ef database update --context BabylonDbContext
```
**Important**: Review the generated migration before applying!

### Step 4: Verify EF Migration
```bash
psql -U postgres -d babylon_db -f 03_VerifyEFMigration.sql
```
Verifies that EF migration created all required columns

### Step 5: Generate Company IDs
```bash
psql -U postgres -d babylon_db -f 04_GenerateCompanyIds.sql
```
Generates UUIDs for all companies

### Step 6: Repopulate Transactions
```bash
psql -U postgres -d babylon_db -f 05_RepopulateTransactions.sql
```
Repopulates transactions table with CompanyId from backup

### Step 7: Repopulate Allocation Strategies
```bash
psql -U postgres -d babylon_db -f 06_RepopulateAllocationStrategies.sql
```
Repopulates allocation_strategies table with CompanyId from backup

### Step 8: Make Columns Non-Nullable
```bash
psql -U postgres -d babylon_db -f 07_MakeColumnsNonNullable.sql
```
Makes CompanyId columns non-nullable

### Step 9: Verification
```bash
psql -U postgres -d babylon_db -f 08_Verification.sql
```
Final verification and summary

### Step 10: Cleanup (Optional - After Verification)
```bash
psql -U postgres -d babylon_db -f 09_CleanupBackupTables.sql
```
Drops backup tables (only run after successful verification)

## Rollback

If something goes wrong, use the rollback script:
```bash
psql -U postgres -d babylon_db -f rollback.sql
```

**Note**: Rollback only works if backup tables still exist and EF migration hasn't been fully applied.

## Verification Checklist

After migration, verify:
- [ ] All companies have non-null `Id` values
- [ ] All transactions have non-null `CompanyId` values
- [ ] All allocation strategies have non-null `CompanyId` values
- [ ] Transaction counts match backup
- [ ] Allocation strategy counts match backup
- [ ] No orphaned records
- [ ] Application starts successfully
- [ ] All unit tests pass
- [ ] API endpoints work correctly

## Troubleshooting

### Error: "CompanyId column not found"
- Ensure Step 3 (EF migration) was run successfully
- Check that migration was applied: `dotnet ef migrations list`

### Error: "Foreign key constraint violation"
- Ensure Step 2 was run to drop old constraints
- Check for any manually created constraints

### Missing tickers in repopulation
- Check backup tables for tickers that don't exist in companies table
- Add missing companies before repopulating

## Notes

- Each script is designed to be run independently
- Scripts include transaction boundaries where appropriate
- All scripts include logging for progress tracking
- Backup tables are kept until cleanup step for safety

