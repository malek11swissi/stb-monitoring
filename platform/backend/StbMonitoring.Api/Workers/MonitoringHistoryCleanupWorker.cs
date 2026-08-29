using Microsoft.EntityFrameworkCore;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Workers;

/// <summary>Supprime les anciens contrôles qui ne servent pas de preuve à une occurrence d'alerte.</summary>
public sealed class MonitoringHistoryCleanupWorker(IServiceScopeFactory scopes,IConfiguration configuration,ILogger<MonitoringHistoryCleanupWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope=scopes.CreateAsyncScope();
                var db=scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
                var retentionDays=Math.Max(30,configuration.GetValue("MonitoringHistory:RetentionDays",180));
                var cutoff=DateTime.UtcNow.AddDays(-retentionDays);
                var deleted=await db.CheckResults.Where(result=>result.StartedAt<cutoff&&!db.AlertOccurrences.Any(occurrence=>occurrence.CheckResultId==result.Id)).ExecuteDeleteAsync(stoppingToken);
                if(deleted>0)logger.LogInformation("{Count} anciens contrôles sans preuve associée ont été supprimés.",deleted);
            }
            catch(Exception exception) when(exception is not OperationCanceledException){logger.LogError(exception,"Échec du nettoyage de l'historique des contrôles.");}
            await Task.Delay(TimeSpan.FromHours(Math.Max(1,configuration.GetValue("MonitoringHistory:CleanupIntervalHours",24))),stoppingToken);
        }
    }
}
