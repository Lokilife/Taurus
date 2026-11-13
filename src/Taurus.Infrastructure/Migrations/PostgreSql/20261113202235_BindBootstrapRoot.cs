using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taurus.Infrastructure.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class BindBootstrapRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "UserLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantRoots",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRoots", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_TenantRoots_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoots_TenantId_UserId",
                table: "TenantRoots",
                columns: new[] { "TenantId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRoots");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "UserLogs");
        }
    }
}
