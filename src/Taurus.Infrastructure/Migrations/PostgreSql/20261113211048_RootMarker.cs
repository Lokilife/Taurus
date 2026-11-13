using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taurus.Infrastructure.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class RootMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Root",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId",
                unique: true,
                filter: "\"Root\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Root",
                table: "Users");
        }
    }
}
