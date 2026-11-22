using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RoleChangeLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "RoleChangeLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RoleChangeLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "RoleChangeLogs");
        }
    }
}
