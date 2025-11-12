# Company → Security Refactoring Plan

## Overview
Renaming `Company` entity/table to `Security` to better reflect that it represents stocks, cryptos, ETFs, bonds, etc.

## Scope of Changes

### 1. Entity & Model Changes
- [ ] Rename `Company.cs` → `Security.cs`
- [ ] Rename class `Company` → `Security`
- [ ] Update namespace if needed
- [ ] Rename property `CompanyId` → `SecurityId` in:
  - `Transaction`
  - `AllocationStrategy`
  - Any other entities

### 2. Database Changes
- [ ] Rename table `companies` → `securities`
- [ ] Rename column `CompanyId` → `SecurityId` in:
  - `transactions` table
  - `allocation_strategies` table
- [ ] Rename foreign key constraints:
  - `FK_transactions_companies_CompanyId` → `FK_transactions_securities_SecurityId`
  - `FK_allocation_strategies_companies_CompanyId` → `FK_allocation_strategies_securities_SecurityId`
- [ ] Rename indexes if they reference the old name

### 3. EF Core Configuration
- [ ] Rename `CompanyConfiguration.cs` → `SecurityConfiguration.cs`
- [ ] Update `TransactionConfiguration.cs` (CompanyId → SecurityId)
- [ ] Update `AllocationStrategyConfiguration.cs` (CompanyId → SecurityId)
- [ ] Update `BabylonDbContext.cs`:
  - `DbSet<Company> Companies` → `DbSet<Security> Securities`

### 4. Repository Layer
- [ ] Rename `ICompanyRepository.cs` → `ISecurityRepository.cs`
- [ ] Rename `CompanyRepository.cs` → `SecurityRepository.cs`
- [ ] Update all method signatures and implementations
- [ ] Update `AllocationStrategyRepository.cs` (Company → Security)
- [ ] Update `MarketPriceRepository.cs` (Company → Security)
- [ ] Update `TransactionRepository.cs` if needed

### 5. Service Layer
- [ ] Rename `ICompanyService.cs` → `ISecurityService.cs`
- [ ] Rename `CompanyService.cs` → `SecurityService.cs`
- [ ] Update `TransactionService.cs`:
  - `CompanyId` → `SecurityId`
  - `Company` → `Security`
- [ ] Update `PortfolioService.cs`:
  - `CompanyId` → `SecurityId`
  - `Company` → `Security`
- [ ] Update `PortfolioInsightsService.cs`
- [ ] Update `AllocationStrategyService.cs`

### 6. Controllers
- [ ] Rename `CompaniesController.cs` → `SecuritiesController.cs`
- [ ] Update route: `/api/companies` → `/api/securities`
- [ ] Update all references

### 7. DTOs & Models
- [ ] Check and update any DTOs that reference `Company`
- [ ] Update request/response models

### 8. Tests
- [ ] Rename `CompanyRepositoryTests.cs` → `SecurityRepositoryTests.cs`
- [ ] Rename `CompanyServiceTests.cs` → `SecurityServiceTests.cs`
- [ ] Rename `CompaniesControllerTests.cs` → `SecuritiesControllerTests.cs`
- [ ] Update all test files that reference `Company` or `CompanyId`
- [ ] Update mocks and test data

### 9. Migrations
- [ ] Create EF Core migration to rename table and columns
- [ ] Update migration scripts in `TickerCompanyIdMigrationPlan/` folder (if still relevant)

### 10. Documentation & Scripts
- [ ] Update SQL scripts that reference `companies` table
- [ ] Update README.md if it mentions Company
- [ ] Update any API documentation

## Migration Strategy

### Step 1: Code Changes (Non-Breaking)
1. Rename entity class and update all C# references
2. Update EF configurations:
   - `CompanyConfiguration.ToTable("companies")` → `SecurityConfiguration.ToTable("securities")`
   - `TransactionConfiguration` - update `CompanyId` → `SecurityId` property mapping
   - `AllocationStrategyConfiguration` - update `CompanyId` → `SecurityId` property mapping
3. Update repositories, services, controllers
4. Update tests
5. **DO NOT** update database yet

### Step 2: Database Migration

**EF Core Migration Approach:**

1. **Generate migration:**
   ```bash
   dotnet ef migrations add RenameCompanyToSecurity --context BabylonDbContext
   ```

2. **CRITICAL: Review the generated migration file**

   EF Core should detect these as **renames** (not drop/add):
   - `migrationBuilder.RenameTable(name: "companies", newName: "securities")`
   - `migrationBuilder.RenameColumn(name: "CompanyId", table: "transactions", newName: "SecurityId")`
   - `migrationBuilder.RenameColumn(name: "CompanyId", table: "allocation_strategies", newName: "SecurityId")`

   **If EF Core generates DROP/ADD instead of RENAME:**
   - **STOP** - Do not apply the migration
   - Manually edit the migration file to use `RenameTable` and `RenameColumn`
   - See "Manual Migration Edit" section below

3. **Apply migration:**
   ```bash
   dotnet ef database update
   ```

### Step 3: Manual Migration Edit (If Needed)

If EF Core doesn't detect renames, manually edit the migration:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Rename table
    migrationBuilder.RenameTable(
        name: "companies",
        newName: "securities");

    // Rename columns
    migrationBuilder.RenameColumn(
        name: "CompanyId",
        table: "transactions",
        newName: "SecurityId");

    migrationBuilder.RenameColumn(
        name: "CompanyId",
        table: "allocation_strategies",
        newName: "SecurityId");

    // Foreign keys will be automatically updated by PostgreSQL
    // But we can explicitly rename them if needed:
    migrationBuilder.RenameIndex(
        name: "IX_transactions_CompanyId",
        table: "transactions",
        newName: "IX_transactions_SecurityId");

    migrationBuilder.RenameIndex(
        name: "IX_allocation_strategies_CompanyId",
        table: "allocation_strategies",
        newName: "IX_allocation_strategies_SecurityId");
}
```

### Step 4: Alternative - Manual SQL Script (If EF Core Fails)

If EF Core migration doesn't work, use this SQL script:

```sql
BEGIN;

-- Rename table
ALTER TABLE companies RENAME TO securities;

-- Rename columns
ALTER TABLE transactions RENAME COLUMN "CompanyId" TO "SecurityId";
ALTER TABLE allocation_strategies RENAME COLUMN "CompanyId" TO "SecurityId";

-- Rename indexes
ALTER INDEX "IX_transactions_CompanyId" RENAME TO "IX_transactions_SecurityId";
ALTER INDEX "IX_allocation_strategies_CompanyId" RENAME TO "IX_allocation_strategies_SecurityId";

-- Foreign key constraints will be automatically updated by PostgreSQL
-- But verify they exist:
-- SELECT conname FROM pg_constraint WHERE conrelid = 'transactions'::regclass;

COMMIT;
```

### Step 5: Verification
- [ ] All tests pass
- [ ] API endpoints work
- [ ] Database schema is correct:
  ```sql
  SELECT table_name FROM information_schema.tables WHERE table_name = 'securities';
  SELECT column_name FROM information_schema.columns WHERE table_name = 'transactions' AND column_name = 'SecurityId';
  ```
- [ ] No broken references

## Files to Update (29+ files)

### Core Entity Files
- `Shared/Data/Models/Company.cs` → `Security.cs`
- `Shared/Data/Configurations/CompanyConfiguration.cs` → `SecurityConfiguration.cs`
- `Shared/Data/Models/Transaction.cs`
- `Shared/Data/Models/AllocationStrategy.cs`
- `Shared/Data/Configurations/TransactionConfiguration.cs`
- `Shared/Data/Configurations/AllocationStrategyConfiguration.cs`
- `Shared/Data/BabylonDbContext.cs`

### Repository Files
- `Shared/Repositories/ICompanyRepository.cs` → `ISecurityRepository.cs`
- `Shared/Repositories/CompanyRepository.cs` → `SecurityRepository.cs`
- `Shared/Repositories/AllocationStrategyRepository.cs`
- `Shared/Repositories/MarketPriceRepository.cs`

### Service Files
- `Features/Investments/Services/ICompanyService.cs` → `ISecurityService.cs`
- `Features/Investments/Services/CompanyService.cs` → `SecurityService.cs`
- `Features/Investments/Services/TransactionService.cs`
- `Features/Investments/Services/PortfolioService.cs`
- `Features/Investments/Services/PortfolioInsightsService.cs`
- `Features/Investments/Services/AllocationStrategyService.cs`

### Controller Files
- `Features/Investments/Controllers/CompaniesController.cs` → `SecuritiesController.cs`

### Test Files
- `Tests/Shared/Repositories/CompanyRepositoryTests.cs` → `SecurityRepositoryTests.cs`
- `Tests/Features/Investments/Services/CompanyServiceTests.cs` → `SecurityServiceTests.cs`
- `Tests/Features/Investments/Controllers/CompaniesControllerTests.cs` → `SecuritiesControllerTests.cs`
- `Tests/Features/Investments/Services/TransactionServiceTests.cs`
- `Tests/Features/Investments/Services/PortfolioServiceTests.cs`
- `Tests/Shared/Repositories/TransactionRepositoryTests.cs`

### Migration Files
- All existing migration `.Designer.cs` files (will be auto-updated)
- `BabylonDbContextModelSnapshot.cs` (will be auto-updated)

## Notes
- This is a breaking change - API routes will change
- Consider API versioning if you have external consumers
- Update Postman collections if you have them
- Update any external documentation

