using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Infrastructure.Persistence;

public sealed class DatabaseSeeder(MonitoringDbContext db, IPasswordService passwords)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Users.AnyAsync(ct))
        {
            var user = new User("admin", "admin@stb.local", passwords.Hash("ChangeMe123!"), "Admin", "STB", RoleNames.Admin);
            db.Users.Add(user);
        }

        if (!await db.AlertRules.AnyAsync(ct))
        {
            db.AlertRules.Add(new AlertRule("Endpoint indisponible", "Déclenche une alerte après un contrôle DOWN.", AlertEventType.EndpointDown, AlertSeverity.Major, 1, 30, true, true));
            db.AlertRules.Add(new AlertRule("Performance dégradée", "Déclenche une alerte après deux contrôles dégradés.", AlertEventType.EndpointDegraded, AlertSeverity.Warning, 2, 30, true, true));
            db.AlertRules.Add(new AlertRule("Timeout réseau", "Déclenche une alerte sur expiration du délai.", AlertEventType.Timeout, AlertSeverity.Major, 1, 30, true, true));
            db.AlertRules.Add(new AlertRule("Certificat TLS invalide", "Déclenche une alerte TLS.", AlertEventType.TlsInvalid, AlertSeverity.Critical, 1, 1440, true, true));
        }
        if (!await db.SlaPolicies.AnyAsync(ct))
        {
            db.SlaPolicies.Add(new SlaPolicy("P1 Critique", "Prise en charge immédiate", IncidentPriority.P1Critical, 15, 60, 25));
            db.SlaPolicies.Add(new SlaPolicy("P2 Haute", "Incident majeur", IncidentPriority.P2High, 30, 240, 25));
            db.SlaPolicies.Add(new SlaPolicy("P3 Moyenne", "Incident standard", IncidentPriority.P3Medium, 120, 480, 25));
            db.SlaPolicies.Add(new SlaPolicy("P4 Faible", "Incident mineur", IncidentPriority.P4Low, 480, 1440, 25));
        }

        await db.SaveChangesAsync(ct);
    }
}
