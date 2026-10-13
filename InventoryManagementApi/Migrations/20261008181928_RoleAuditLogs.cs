using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class RoleAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PromoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    OldRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NewRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TimeStampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleChangeLogs_Users_PromoterId",
                        column: x => x.PromoterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleChangeLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeLogs_PromoterId",
                table: "RoleChangeLogs",
                column: "PromoterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeLogs_TargetUserId",
                table: "RoleChangeLogs",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleChangeLogs");
        }
    }
}
