using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incident_escalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    NextEscalationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastResult = table.Column<string>(type: "text", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_escalations");
        }
    }
}
