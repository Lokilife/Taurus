using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taurus.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminPolicies",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RequiredFactors = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedFactors = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPolicies", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "GroupEntity",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupEntity", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Schemas",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Definition = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schemas", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "UserLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogs", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Locale = table.Column<string>(type: "TEXT", nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Attributes",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => new { x.TenantId, x.UserId, x.Name, x.Position });
                    table.ForeignKey(
                        name: "FK_Attributes_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Credentials_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembershipEntity",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembershipEntity", x => new { x.TenantId, x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMembershipEntity_GroupEntity_TenantId_GroupId",
                        columns: x => new { x.TenantId, x.GroupId },
                        principalTable: "GroupEntity",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembershipEntity_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TokenDigest = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IdleExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsoluteExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Revoked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => new { x.TenantId, x.TokenDigest });
                    table.ForeignKey(
                        name: "FK_Sessions_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPolicies",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequiredFactors = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicies", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserPolicies_Users_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_TenantId_UserId",
                table: "Credentials",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupEntity_TenantId_Name",
                table: "GroupEntity",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembershipEntity_TenantId_UserId",
                table: "GroupMembershipEntity",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TenantId_Id",
                table: "Sessions",
                columns: new[] { "TenantId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TenantId_UserId",
                table: "Sessions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLogs_TenantId_Timestamp",
                table: "UserLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Username",
                table: "Users",
                columns: new[] { "TenantId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminPolicies");

            migrationBuilder.DropTable(
                name: "Attributes");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "GroupMembershipEntity");

            migrationBuilder.DropTable(
                name: "Schemas");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "UserLogs");

            migrationBuilder.DropTable(
                name: "UserPolicies");

            migrationBuilder.DropTable(
                name: "GroupEntity");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
