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

            migrationBuilder.DropPrimaryKey(
                name: "PK_companies",
                table: "companies");

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

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "companies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
