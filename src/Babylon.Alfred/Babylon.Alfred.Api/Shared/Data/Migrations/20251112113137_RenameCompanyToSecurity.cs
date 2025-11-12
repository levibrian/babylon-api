using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCompanyToSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign keys (they will be recreated with new names)
            migrationBuilder.DropForeignKey(
                name: "FK_allocation_strategies_companies_CompanyId",
                table: "allocation_strategies");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_companies_CompanyId",
                table: "transactions");

            // Step 2: Rename table (preserves all data)
            // Note: PostgreSQL does NOT automatically rename constraints/indexes when renaming tables
            migrationBuilder.RenameTable(
                name: "companies",
                newName: "securities");

            // Step 2b: Rename primary key constraint explicitly (PostgreSQL doesn't auto-rename it)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'PK_companies'
                        AND conrelid = 'securities'::regclass
                    ) THEN
                        ALTER TABLE securities RENAME CONSTRAINT ""PK_companies"" TO ""PK_securities"";
                    END IF;
                END $$;
            ");

            // Step 3: Rename columns (preserves all data)
            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "transactions",
                newName: "SecurityId");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "allocation_strategies",
                newName: "SecurityId");

            // Step 4: Rename indexes
            migrationBuilder.RenameIndex(
                name: "IX_transactions_CompanyId",
                table: "transactions",
                newName: "IX_transactions_SecurityId");

            migrationBuilder.RenameIndex(
                name: "IX_allocation_strategies_UserId_CompanyId",
                table: "allocation_strategies",
                newName: "IX_allocation_strategies_UserId_SecurityId");

            migrationBuilder.RenameIndex(
                name: "IX_allocation_strategies_CompanyId",
                table: "allocation_strategies",
                newName: "IX_allocation_strategies_SecurityId");

            // Step 5: Rename primary key constraint
            migrationBuilder.RenameIndex(
                name: "IX_companies_Ticker",
                table: "securities",
                newName: "IX_securities_Ticker");

            // Step 6: Recreate foreign keys with new names (PostgreSQL will auto-update references)
            migrationBuilder.AddForeignKey(
                name: "FK_allocation_strategies_securities_SecurityId",
                table: "allocation_strategies",
                column: "SecurityId",
                principalTable: "securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_securities_SecurityId",
                table: "transactions",
                column: "SecurityId",
                principalTable: "securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_allocation_strategies_securities_SecurityId",
                table: "allocation_strategies");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_securities_SecurityId",
                table: "transactions");

            // Step 2: Rename indexes back
            migrationBuilder.RenameIndex(
                name: "IX_securities_Ticker",
                table: "securities",
                newName: "IX_companies_Ticker");

            migrationBuilder.RenameIndex(
                name: "IX_allocation_strategies_SecurityId",
                table: "allocation_strategies",
                newName: "IX_allocation_strategies_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_allocation_strategies_UserId_SecurityId",
                table: "allocation_strategies",
                newName: "IX_allocation_strategies_UserId_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_SecurityId",
                table: "transactions",
                newName: "IX_transactions_CompanyId");

            // Step 3: Rename columns back
            migrationBuilder.RenameColumn(
                name: "SecurityId",
                table: "allocation_strategies",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "SecurityId",
                table: "transactions",
                newName: "CompanyId");

            // Step 4: Rename table back (preserves all data)
            migrationBuilder.RenameTable(
                name: "securities",
                newName: "companies");

            // Step 4b: Rename primary key constraint back
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'PK_securities'
                        AND conrelid = 'companies'::regclass
                    ) THEN
                        ALTER TABLE companies RENAME CONSTRAINT ""PK_securities"" TO ""PK_companies"";
                    END IF;
                END $$;
            ");

            // Step 5: Recreate foreign keys with old names
            migrationBuilder.AddForeignKey(
                name: "FK_allocation_strategies_companies_CompanyId",
                table: "allocation_strategies",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_companies_CompanyId",
                table: "transactions",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
