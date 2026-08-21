using Microsoft.EntityFrameworkCore;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Workers;

/// <summary>
/// Applique la politique de conservation des journaux d'audit. Une rétention
/// minimale de 30 jours protège la traçabilité contre une mauvaise configuration.
/// </summary>
public sealed class AuditCleanupWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AuditCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), ct);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // La valeur par défaut conserve un an d'audit ; elle reste configurable.
                var retentionDays = Math.Max(30, configuration.GetValue("Audit:RetentionDays", 365));
                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
                var deleted = await db.AuditLogs.Where(x => x.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
                if (deleted > 0) logger.LogInformation("{Count} anciens audits supprimés (rétention : {Days} jours).", deleted, retentionDays);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Échec du nettoyage automatique des audits."); }

            await Task.Delay(TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Audit:CleanupIntervalHours", 24))), ct);
        }
    }
}
