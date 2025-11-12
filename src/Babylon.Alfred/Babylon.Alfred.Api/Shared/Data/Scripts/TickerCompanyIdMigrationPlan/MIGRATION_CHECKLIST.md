# Migration Checklist: Ticker → CompanyId Foreign Key Change

## Overview
Migrating from using `Ticker` as a foreign key to using `CompanyId` (Guid) as the foreign key in `Transaction` and `AllocationStrategy` tables. This allows supporting multiple exchanges for the same company (e.g., AAPL on NYSE vs AAPL on EU exchanges).

---

## ✅ Code Changes (COMPLETED)

### 1. Entity Models
- [x] **Company.cs**: Changed primary key from `Ticker` to `Id` (Guid)
- [x] **Transaction.cs**: Changed `Ticker` property to `CompanyId` (Guid)
- [x] **AllocationStrategy.cs**: Changed `Ticker` property to `CompanyId` (Guid)
- [x] **MarketPrice.cs**: Still uses `Ticker` (correct - it's not a FK, just stores ticker strings)

### 2. Entity Framework Configurations
- [x] **CompanyConfiguration.cs**: 
  - Primary key set to `Id`
  - Unique index on `Ticker` (allows lookup by ticker)
  - `Id` configured with `ValueGeneratedOnAdd()`
- [x] **TransactionConfiguration.cs**: 
  - Foreign key changed to `CompanyId`
  - Index on `CompanyId`
  - Navigation property to `Company`
- [x] **AllocationStrategyConfiguration.cs**: 
  - Foreign key changed to `CompanyId`
  - Unique index on `(UserId, CompanyId)` instead of `(UserId, Ticker)`
  - Navigation property to `Company`

### 3. Repositories
- [x] **CompanyRepository.cs**: 
  - Added `GetByIdsAsync()` method
  - `AddOrUpdateAsync()` updated to find by Ticker (unique index) and generate Id if needed
- [x] **TransactionRepository.cs**: No changes needed (uses CompanyId from entity)
- [x] **AllocationStrategyRepository.cs**: 
  - `GetTargetAllocationsByUserIdAsync()` uses `Company.Ticker` via navigation
  - `SetAllocationStrategyAsync()` uses `CompanyId` for comparison
  - `GetDistinctCompanyIdsAsync()` returns `List<Guid>` instead of `List<string>`
- [x] **MarketPriceRepository.cs**: 
  - `GetTickersNeedingUpdateAsync()` uses `Company.Ticker` via navigation

### 4. Services
- [x] **TransactionService.cs**: 
  - `CreateTransaction()` takes `companyId` parameter
  - `CreateBulk()` fetches companies and maps tickers to CompanyIds
- [x] **PortfolioService.cs**: 
  - Groups transactions by `CompanyId` instead of `Ticker`
  - Uses `GetByIdsAsync()` to fetch companies
  - Gets ticker from Company navigation property
- [x] **PortfolioInsightsService.cs**: 
  - Groups transactions by `CompanyId`
  - Loads companies to get tickers
- [x] **AllocationStrategyService.cs**: 
  - Converts `Ticker` → `CompanyId` when setting allocations
  - Validates companies exist before creating allocations

### 5. Tests
- [x] **TransactionServiceTests.cs**: Updated to use `CompanyId`
- [x] **TransactionRepositoryTests.cs**: Updated to create companies with `Id` and use `CompanyId`
- [x] **PortfolioServiceTests.cs**: Updated to use `CompanyId` and mock `GetByIdsAsync`
- [x] **CompanyRepositoryTests.cs**: Updated to use `FirstOrDefaultAsync` instead of `FindAsync` with string
- [x] All tests mock `IMarketPriceService` and `IAllocationStrategyService`

---

## ⚠️ Database Migration (PENDING)

### Step 1: Pre-Migration Backup ✅
- [x] SQL script creates backup tables (`transactions_backup`, `allocation_strategies_backup`)
- [x] Script logs backup completion with counts

### Step 2: Drop FK Constraints and Delete Data ✅
- [x] SQL script drops FK constraint `FK_transactions_companies_Ticker` if it exists
- [x] SQL script drops any FK constraint on `Ticker` column in `allocation_strategies` if it exists
- [x] SQL script deletes all data from `transactions` table
- [x] SQL script deletes all data from `allocation_strategies` table
- [x] Script logs deletion completion

### Step 3: EF Core Migration ⚠️ **NEEDS TO BE CREATED**
- [ ] **ACTION REQUIRED**: Run `dotnet ef migrations add ChangeToCompanyIdForeignKey --context BabylonDbContext`
- [ ] Migration should:
  - Add `Id` column (Guid) to `companies` table (if not exists)
  - Add `CompanyId` column (Guid, nullable initially) to `transactions` table
  - Add `CompanyId` column (Guid, nullable initially) to `allocation_strategies` table
  - Drop old foreign key constraints on `Ticker`
  - Drop old indexes on `Ticker` in `transactions` and `allocation_strategies`
  - Add new foreign key constraints on `CompanyId`
  - Add new indexes on `CompanyId`
  - **DO NOT** drop the `Ticker` column from `transactions` or `allocation_strategies` yet (for rollback safety)

### Step 4: Generate Company IDs ✅
- [x] SQL script generates UUIDs for all companies that don't have one
- [x] Script verifies all companies have IDs

### Step 5: Repopulate Data ✅
- [x] SQL script repopulates `transactions` with `CompanyId` from backup
- [x] SQL script repopulates `allocation_strategies` with `CompanyId` from backup
- [x] Script handles missing tickers gracefully (warnings, not errors)
- [x] Script logs repopulation counts

### Step 6: Make Columns Non-Nullable ✅
- [x] SQL script makes `CompanyId` non-nullable in `transactions`
- [x] SQL script makes `CompanyId` non-nullable in `allocation_strategies`

### Step 7: Verification ✅
- [x] SQL script verifies:
  - All companies have IDs
  - No NULL `CompanyId` values in transactions
  - No NULL `CompanyId` values in allocation_strategies
  - Counts match expectations

### Step 8: Cleanup (Optional) ⚠️
- [ ] After successful migration and verification:
  - Drop `Ticker` column from `transactions` table (if desired)
  - Drop `Ticker` column from `allocation_strategies` table (if desired)
  - Drop backup tables (commented out in script)

---

## 🔍 Migration Script Review

### Current Scripts: `TickerCompanyIdMigrationPlan/`

**Strengths:**
- ✅ Comprehensive backup strategy
- ✅ Modular step-by-step scripts
- ✅ Transactional (wrapped in BEGIN/COMMIT)
- ✅ Detailed logging at each step
- ✅ Verification checks
- ✅ Handles missing tickers gracefully
- ✅ Rollback instructions included
- ✅ EF migration verification step

**Potential Issues:**
1. ⚠️ **Step 3 Gap**: Script assumes EF migration will be run manually between Step 2 and Step 4
   - **Status**: ✅ Handled - Script explicitly drops FK constraints before deleting data
2. ⚠️ **Ticker Column**: Old `Ticker` columns remain in tables (good for rollback, but should be cleaned up later)
   - **Recommendation**: Drop `Ticker` columns in a follow-up migration after verification

**Recommendations:**
1. ✅ **DONE**: FK constraint dropping added to SQL script (Step 2)
2. ✅ **DONE**: Added verification step to check EF migration was run (checks for `CompanyId` and `Id` columns)
3. ✅ **DONE**: Backup tables preserve `Ticker` columns for rollback safety

---

## 📋 Migration Execution Order

1. **Backup Database** (manual step - not in script)
   ```bash
   pg_dump -U postgres -d babylon_db > backup_before_migration_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Run SQL Script Step 1** (backup)
   ```bash
   psql -U postgres -d babylon_db -f 01_BackupData.sql
   ```

3. **Run SQL Script Step 2** (drop constraints and delete data)
   ```bash
   psql -U postgres -d babylon_db -f 02_DropConstraintsAndDeleteData.sql
   ```

4. **Create and Apply EF Migration**
   ```bash
   cd src/Babylon.Alfred/Babylon.Alfred.Api
   dotnet ef migrations add ChangeToCompanyIdForeignKey --context BabylonDbContext
   dotnet ef database update --context BabylonDbContext
   ```

5. **Run SQL Script Step 3** (verify EF migration)
   ```bash
   psql -U postgres -d babylon_db -f 03_VerifyEFMigration.sql
   ```

6. **Run SQL Script Step 4** (generate company IDs)
   ```bash
   psql -U postgres -d babylon_db -f 04_GenerateCompanyIds.sql
   ```

7. **Run SQL Script Step 5** (repopulate transactions)
   ```bash
   psql -U postgres -d babylon_db -f 05_RepopulateTransactions.sql
   ```

8. **Run SQL Script Step 6** (repopulate allocation strategies)
   ```bash
   psql -U postgres -d babylon_db -f 06_RepopulateAllocationStrategies.sql
   ```

9. **Run SQL Script Step 7** (make columns non-nullable)
   ```bash
   psql -U postgres -d babylon_db -f 07_MakeColumnsNonNullable.sql
   ```

10. **Run SQL Script Step 8** (verification)
    ```bash
    psql -U postgres -d babylon_db -f 08_Verification.sql
    ```

11. **Run SQL Script Step 9** (cleanup - optional, after verification)
    ```bash
    psql -U postgres -d babylon_db -f 09_CleanupBackupTables.sql
    ```

---

## 🚨 Risk Assessment

### High Risk Areas:
1. **Data Loss**: If migration fails between Step 2 and Step 5, data is deleted but not repopulated
   - **Mitigation**: Comprehensive backups, transactional scripts, rollback instructions

2. **Missing Tickers**: If transactions/allocation strategies reference tickers not in companies table
   - **Mitigation**: Script handles gracefully with warnings, doesn't fail migration

3. **EF Migration Conflicts**: If EF migration tries to create columns that already exist
   - **Mitigation**: Review migration before applying, ensure it's idempotent

### Medium Risk Areas:
1. **Concurrent Access**: If application is running during migration
   - **Mitigation**: Stop application during migration, or use maintenance mode

2. **Large Datasets**: Migration might be slow with many transactions
   - **Mitigation**: Test on staging first, consider batching if needed

---

## ✅ Verification Checklist

After migration, verify:
- [ ] All companies have non-null `Id` values
- [ ] All transactions have non-null `CompanyId` values
- [ ] All allocation strategies have non-null `CompanyId` values
- [ ] Transaction counts match backup
- [ ] Allocation strategy counts match backup
- [ ] No orphaned records (CompanyId pointing to non-existent companies)
- [ ] Application starts successfully
- [ ] All unit tests pass
- [ ] API endpoints work correctly
- [ ] Portfolio calculations work correctly
- [ ] Allocation strategy endpoints work correctly

---

## 📝 Notes

- **MarketPrice table**: Still uses `Ticker` as a string field (not a FK). This is correct - it stores ticker symbols for price lookups.
- **Company.Ticker**: Remains as a unique indexed field for lookups, but is no longer the primary key.
- **Rollback Strategy**: Backup tables are kept until migration is verified. Rollback script is included in `rollback.sql`.
- **Script Organization**: All migration scripts are organized in `TickerCompanyIdMigrationPlan/` folder for easy execution.

