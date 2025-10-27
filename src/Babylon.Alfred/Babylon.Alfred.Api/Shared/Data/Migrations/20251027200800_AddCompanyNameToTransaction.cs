using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyNameToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "transactions");
        }
    }
}
