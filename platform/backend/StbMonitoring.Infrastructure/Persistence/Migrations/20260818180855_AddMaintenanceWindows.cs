using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_windows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SuppressAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_windows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_windows_monitored_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "monitored_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_maintenance_windows_monitoring_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "monitoring_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_maintenance_windows_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_windows_CreatedByUserId",
                table: "maintenance_windows",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_windows_EndpointId",
                table: "maintenance_windows",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_windows_StartsAt_EndsAt_IsCancelled",
                table: "maintenance_windows",
                columns: new[] { "StartsAt", "EndsAt", "IsCancelled" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_windows_SystemId",
                table: "maintenance_windows",
                column: "SystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_windows");
        }
    }
}
