using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdAsPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_companies_Ticker",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_Ticker",
                table: "transactions");

            // Drop primary key conditionally (in case migration partially ran before)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'PK_companies'
                        AND conrelid = 'companies'::regclass
                    ) THEN
                        ALTER TABLE companies DROP CONSTRAINT ""PK_companies"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropIndex(
                name: "IX_allocation_strategies_UserId_Ticker",
                table: "allocation_strategies");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "allocation_strategies");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Add Id column as nullable first (if it doesn't already exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'companies' AND column_name = 'Id'
                    ) THEN
                        ALTER TABLE companies ADD COLUMN ""Id"" uuid;
                    END IF;
                END $$;
            ");

            // Generate unique UUIDs for all companies (handles NULL and duplicate values)
            migrationBuilder.Sql(@"
                -- Fix NULL or empty GUID values
                UPDATE companies
                SET ""Id"" = gen_random_uuid()
                WHERE ""Id"" IS NULL OR ""Id"" = '00000000-0000-0000-0000-000000000000'::uuid;

                -- Handle any remaining duplicates by regenerating UUIDs (keep the first occurrence, regenerate others)
                DO $$
                DECLARE
                    rec RECORD;
                    new_id uuid;
                BEGIN
                    FOR rec IN
                        SELECT ""Ticker"", ""Id"",
                               ROW_NUMBER() OVER (PARTITION BY ""Id"" ORDER BY ""Ticker"") as rn
                        FROM companies
                        WHERE ""Id"" IN (
                            SELECT ""Id"" FROM companies
                            GROUP BY ""Id""
                            HAVING COUNT(*) > 1
                        )
                    LOOP
                        IF rec.rn > 1 THEN
                            new_id := gen_random_uuid();
                            UPDATE companies SET ""Id"" = new_id WHERE ""Ticker"" = rec.""Ticker"";
                        END IF;
                    END LOOP;
                END $$;
            ");

            // Make Id column non-nullable using raw SQL
            migrationBuilder.Sql(@"
                ALTER TABLE companies ALTER COLUMN ""Id"" SET NOT NULL;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "allocation_strategies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_companies",
                table: "companies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_CompanyId",
                table: "transactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_Ticker",
                table: "companies",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_allocation_strategies_CompanyId",
                table: "allocation_strategies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_allocation_strategies_UserId_CompanyId",
                table: "allocation_strategies",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_allocation_strategies_companies_CompanyId",
                table: "allocation_strategies");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_companies_CompanyId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_CompanyId",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_companies",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_companies_Ticker",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_allocation_strategies_CompanyId",
                table: "allocation_strategies");

            migrationBuilder.DropIndex(
                name: "IX_allocation_strategies_UserId_CompanyId",
                table: "allocation_strategies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "allocation_strategies");

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "allocation_strategies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_companies",
                table: "companies",
                column: "Ticker");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Ticker",
                table: "transactions",
                column: "Ticker");

            migrationBuilder.CreateIndex(
                name: "IX_allocation_strategies_UserId_Ticker",
                table: "allocation_strategies",
                columns: new[] { "UserId", "Ticker" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_companies_Ticker",
                table: "transactions",
                column: "Ticker",
                principalTable: "companies",
                principalColumn: "Ticker",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
