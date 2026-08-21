using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StbMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperationalFeatureBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectiveAction",
                table: "incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreventiveAction",
                table: "incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionEvidence",
                table: "incidents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "incident_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsResolutionProof = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incident_attachments_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_attachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_views_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    CriticalOnly = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationDelayMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incident_attachments_IncidentId",
                table: "incident_attachments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_attachments_UploadedByUserId",
                table: "incident_attachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_views_UserId_Scope",
                table: "saved_views",
                columns: new[] { "UserId", "Scope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_attachments");

            migrationBuilder.DropTable(
                name: "saved_views");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropColumn(
                name: "CorrectiveAction",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "PreventiveAction",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "ResolutionEvidence",
                table: "incidents");
        }
    }
}
