using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Babylon.Alfred.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityTypeToSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "SecurityType",
                table: "securities",
                type: "integer",
                nullable: true);
            
            // Set default value for existing records (1 = Stock)
            migrationBuilder.Sql("UPDATE securities SET \"SecurityType\" = 1 WHERE \"SecurityType\" IS NULL");
            
            // Make the column non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "SecurityType",
                table: "securities",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityType",
                table: "securities");
        }
    }
}
