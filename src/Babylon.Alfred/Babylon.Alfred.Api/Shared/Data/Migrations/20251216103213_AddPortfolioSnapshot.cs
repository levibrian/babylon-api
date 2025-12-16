using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portfolio_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalMarketValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnrealizedPnLPercentage = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_portfolio_snapshots_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_snapshots_SnapshotDate",
                table: "portfolio_snapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_snapshots_UserId_SnapshotDate",
                table: "portfolio_snapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portfolio_snapshots");
        }
    }
}
