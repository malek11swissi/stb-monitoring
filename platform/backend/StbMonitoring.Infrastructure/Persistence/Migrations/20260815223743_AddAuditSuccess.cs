using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSuccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "audit_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Success",
                table: "audit_logs");
        }
    }
}
