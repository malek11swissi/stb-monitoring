using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2Monitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitored_systems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Criticality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: true),
                    MonitoringEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitored_systems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monitoring_endpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CheckType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    ExpectedStatusCode = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    DegradedThresholdMs = table.Column<int>(type: "integer", nullable: false),
                    DownThresholdMs = table.Column<int>(type: "integer", nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedJsonProperty = table.Column<string>(type: "text", nullable: true),
                    ExpectedJsonValue = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitoring_endpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_monitoring_endpoints_monitored_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "monitored_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ErrorType = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TriggeredManually = table.Column<bool>(type: "boolean", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_results_monitored_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "monitored_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_check_results_monitoring_endpoints_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "monitoring_endpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_results_EndpointId_StartedAt",
                table: "check_results",
                columns: new[] { "EndpointId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_check_results_SystemId",
                table: "check_results",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_monitored_systems_Code",
                table: "monitored_systems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monitoring_endpoints_SystemId",
                table: "monitoring_endpoints",
                column: "SystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_results");

            migrationBuilder.DropTable(
                name: "monitoring_endpoints");

            migrationBuilder.DropTable(
                name: "monitored_systems");
        }
    }
}
