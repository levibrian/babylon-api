using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyInvestmentAmount",
                table: "users",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabledForBiWeekly",
                table: "allocation_strategies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabledForMonthly",
                table: "allocation_strategies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabledForWeekly",
                table: "allocation_strategies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyInvestmentAmount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEnabledForBiWeekly",
                table: "allocation_strategies");

            migrationBuilder.DropColumn(
                name: "IsEnabledForMonthly",
                table: "allocation_strategies");

            migrationBuilder.DropColumn(
                name: "IsEnabledForWeekly",
                table: "allocation_strategies");
        }
    }
}
