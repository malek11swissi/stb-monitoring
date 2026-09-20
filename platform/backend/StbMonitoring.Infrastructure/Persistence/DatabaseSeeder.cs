using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Infrastructure.Persistence;

/// <summary>Initialise les comptes, SI, endpoints, règles et SLA standards de démonstration.</summary>
public sealed class DatabaseSeeder(MonitoringDbContext db, IPasswordService passwords)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        // Les installations existantes utilisent EnsureCreated (sans historique EF).
        // Ajouter uniquement les colonnes manquantes, sans recréer ni effacer la base.
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DatabaseEngine" character varying(20);
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DatabaseHost" character varying(253);
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DatabasePort" integer;
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DatabaseName" character varying(120);
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DatabaseGroup" character varying(100);
            ALTER TABLE monitoring_endpoints ADD COLUMN IF NOT EXISTS "DeclaredRole" character varying(20);
            CREATE INDEX IF NOT EXISTS "IX_monitoring_endpoints_SystemId_DatabaseGroup"
                ON monitoring_endpoints ("SystemId", "DatabaseGroup");
            """, ct);

        if (!await db.Users.AnyAsync(ct))
        {
            var user = new User("admin", "admin@stb.local", passwords.Hash("ChangeMe123!"), "Admin", "STB", RoleNames.Admin);
            db.Users.Add(user);
        }

        await EnsureRule("Endpoint indisponible","Contrôle DOWN.",AlertEventType.EndpointDown,AlertSeverity.Major,2,15,ct);
        await EnsureRule("Performance dégradée","Latence supérieure au seuil.",AlertEventType.EndpointDegraded,AlertSeverity.Warning,2,15,ct);
        await EnsureRule("Timeout réseau","Expiration du délai.",AlertEventType.Timeout,AlertSeverity.Major,1,15,ct);
        await EnsureRule("Réponse API invalide","Code HTTP ou contenu inattendu.",AlertEventType.ValidationFailed,AlertSeverity.Major,1,15,ct);
        await EnsureRule("Certificat bientôt expiré","Expiration dans moins de 30 jours.",AlertEventType.TlsExpiring,AlertSeverity.Warning,1,1440,ct);
        await EnsureRule("Certificat TLS expiré","Date de validité dépassée.",AlertEventType.TlsExpired,AlertSeverity.Critical,1,1440,ct);
        await EnsureRule("Certificat TLS invalide","Chaîne, hôte ou négociation TLS invalide.",AlertEventType.TlsInvalid,AlertSeverity.Critical,1,60,ct);
        if (!await db.SlaPolicies.AnyAsync(ct))
        {
            db.SlaPolicies.Add(new SlaPolicy("P1 Critique", "Prise en charge immédiate", IncidentPriority.P1Critical, 15, 60, 25));
            db.SlaPolicies.Add(new SlaPolicy("P2 Haute", "Incident majeur", IncidentPriority.P2High, 30, 240, 25));
            db.SlaPolicies.Add(new SlaPolicy("P3 Moyenne", "Incident standard", IncidentPriority.P3Medium, 120, 480, 25));
            db.SlaPolicies.Add(new SlaPolicy("P4 Faible", "Incident mineur", IncidentPriority.P4Low, 480, 1440, 25));
        }

        await CorrectKnownEndpoints(ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureRule(string name,string description,AlertEventType type,AlertSeverity severity,int failures,int dedup,CancellationToken ct)
    {if(!await db.AlertRules.AnyAsync(x=>x.EventType==type&&!x.SystemId.HasValue&&!x.EndpointId.HasValue,ct))db.AlertRules.Add(new(name,description,type,severity,failures,dedup,true,true));}
    private async Task CorrectKnownEndpoints(CancellationToken ct)
    {
        foreach(var e in await db.MonitoringEndpoints.Where(x=>x.Name=="SMS LIVRAISON"||x.Name=="VERIFICATION TLS SMS"||x.Name=="situation-professionnelle").ToArrayAsync(ct))
        {
            var url=e.Name switch{"SMS LIVRAISON"=>"http://localhost:5103/api/v1/messages/SMS-STB-001/statut-livraison","VERIFICATION TLS SMS"=>"https://localhost:7103","situation-professionnelle"=>"http://localhost:5104/api/v1/employes/STB-TECH-001/situation-professionnelle",_=>e.Url};
            var type=e.Name.StartsWith("VERIFICATION TLS")?CheckType.Tls:e.Name=="SMS LIVRAISON"?CheckType.ApiJson:e.CheckType;
            e.Update(e.Name,url,type,e.HttpMethod,e.ExpectedStatusCode,e.TimeoutSeconds,e.IntervalSeconds,e.DegradedThresholdMs,e.DownThresholdMs,e.IsCritical,e.Name=="SMS LIVRAISON"?"serviceSmsAccessible":e.ExpectedJsonProperty,e.Name=="SMS LIVRAISON"?"true":e.ExpectedJsonValue);
        }
    }
}
