using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MonitoringDbContext))]
[Migration("20260918090000_AddDatabaseMonitoringEndpoints")]
public sealed class AddDatabaseMonitoringEndpoints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DatabaseEngine", "monitoring_endpoints", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>("DatabaseHost", "monitoring_endpoints", type: "character varying(253)", maxLength: 253, nullable: true);
        migrationBuilder.AddColumn<int>("DatabasePort", "monitoring_endpoints", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>("DatabaseName", "monitoring_endpoints", type: "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>("DatabaseGroup", "monitoring_endpoints", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("DeclaredRole", "monitoring_endpoints", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.CreateIndex("IX_monitoring_endpoints_SystemId_DatabaseGroup", "monitoring_endpoints", new[] { "SystemId", "DatabaseGroup" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_monitoring_endpoints_SystemId_DatabaseGroup", "monitoring_endpoints");
        migrationBuilder.DropColumn("DatabaseEngine", "monitoring_endpoints");
        migrationBuilder.DropColumn("DatabaseHost", "monitoring_endpoints");
        migrationBuilder.DropColumn("DatabasePort", "monitoring_endpoints");
        migrationBuilder.DropColumn("DatabaseName", "monitoring_endpoints");
        migrationBuilder.DropColumn("DatabaseGroup", "monitoring_endpoints");
        migrationBuilder.DropColumn("DeclaredRole", "monitoring_endpoints");
    }
}
