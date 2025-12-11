using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeMarketPriceToSecurityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing market prices - they will be repopulated by the worker
            // This is a cache table, so data loss is acceptable
            migrationBuilder.Sql("DELETE FROM market_prices;");

            migrationBuilder.DropIndex(
                name: "IX_market_prices_Ticker",
                table: "market_prices");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "market_prices");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "market_prices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityId",
                table: "market_prices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_market_prices_SecurityId",
                table: "market_prices",
                column: "SecurityId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_market_prices_securities_SecurityId",
                table: "market_prices",
                column: "SecurityId",
                principalTable: "securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear market prices before reverting schema
            migrationBuilder.Sql("DELETE FROM market_prices;");

            migrationBuilder.DropForeignKey(
                name: "FK_market_prices_securities_SecurityId",
                table: "market_prices");

            migrationBuilder.DropIndex(
                name: "IX_market_prices_SecurityId",
                table: "market_prices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "market_prices");

            migrationBuilder.DropColumn(
                name: "SecurityId",
                table: "market_prices");

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "market_prices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_market_prices_Ticker",
                table: "market_prices",
                column: "Ticker");
        }
    }
}
