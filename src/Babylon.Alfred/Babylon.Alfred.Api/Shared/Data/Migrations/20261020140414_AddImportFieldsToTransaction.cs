using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportFieldsToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportFingerprint",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportSource",
                table: "transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UserId_ImportFingerprint",
                table: "transactions",
                columns: new[] { "UserId", "ImportFingerprint" },
                unique: true,
                filter: "\"ImportFingerprint\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_UserId_ImportFingerprint",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ImportFingerprint",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ImportSource",
                table: "transactions");
        }
    }
}
