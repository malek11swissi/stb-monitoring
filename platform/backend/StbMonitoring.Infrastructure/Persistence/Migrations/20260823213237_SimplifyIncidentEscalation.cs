using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyIncidentEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_escalations");

            migrationBuilder.DropColumn(
                name: "EscalationDelayMinutes",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscalationDelayMinutes",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "incident_escalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastResult = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    NextEscalationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_escalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incident_escalations_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incident_escalations_CompletedAt_NextEscalationAt",
                table: "incident_escalations",
                columns: new[] { "CompletedAt", "NextEscalationAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_escalations_IncidentId",
                table: "incident_escalations",
                column: "IncidentId",
                unique: true);
        }
    }
}
