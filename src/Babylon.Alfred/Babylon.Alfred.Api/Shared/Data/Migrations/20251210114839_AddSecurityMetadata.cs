using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Geography",
                table: "securities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "securities",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "securities",
                type: "numeric(20,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "securities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Geography",
                table: "securities");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "securities");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "securities");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "securities");
        }
    }
}
