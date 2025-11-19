using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column as nullable first
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "transactions",
                type: "timestamp with time zone",
                nullable: true);

            // Set UpdatedAt = Date for all existing records
            migrationBuilder.Sql("UPDATE transactions SET \"UpdatedAt\" = \"Date\" WHERE \"UpdatedAt\" IS NULL");

            // Make the column non-nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "transactions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "transactions");
        }
    }
}
