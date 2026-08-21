using System.Data;
using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Workers;

/// <summary>
/// Déclenche périodiquement les contrôles arrivés à échéance. Le verrou
/// consultatif PostgreSQL garantit qu'une seule instance de l'API travaille à
/// la fois, même lorsque l'application est répliquée dans Kubernetes.
/// </summary>
public sealed class MonitoringWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<MonitoringWorker> logger) : BackgroundService
{
    // Identifiant stable partagé par toutes les instances de STB Sentinel.
    // Ce verrou ne bloque aucune table et est libéré à la fin de chaque cycle.
    private const long AdvisoryLockId = 7_321_504_101;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync(ct);
                try
                {
                    await using var acquire = connection.CreateCommand();
                    acquire.CommandText = "SELECT pg_try_advisory_lock(@lock_id)";
                    var parameter = acquire.CreateParameter();
                    parameter.ParameterName = "lock_id";
                    parameter.Value = AdvisoryLockId;
                    acquire.Parameters.Add(parameter);
                    var ownsLock = Convert.ToBoolean(await acquire.ExecuteScalarAsync(ct));
                    if (!ownsLock)
                    {
                        logger.LogDebug("Une autre instance exécute déjà le cycle de monitoring.");
                    }
                    else
                    {
                        try
                        {
                            // ExecuteDueChecksAsync sélectionne les endpoints selon NextCheckAt ;
                            // le worker n'a donc aucune règle métier sur les fréquences.
                            var count = await scope.ServiceProvider.GetRequiredService<IMonitoringService>().ExecuteDueChecksAsync(ct);
                            if (count > 0) logger.LogInformation("{Count} contrôles de supervision exécutés.", count);
                        }
                        finally
                        {
                            // La libération explicite évite de conserver le verrou tant que
                            // la connexion reste dans le pool PostgreSQL.
                            await using var release = connection.CreateCommand();
                            release.CommandText = "SELECT pg_advisory_unlock(@lock_id)";
                            var releaseParameter = release.CreateParameter();
                            releaseParameter.ParameterName = "lock_id";
                            releaseParameter.Value = AdvisoryLockId;
                            release.Parameters.Add(releaseParameter);
                            await release.ExecuteScalarAsync(CancellationToken.None);
                        }
                    }
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed) await connection.CloseAsync();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Erreur du worker de supervision."); }

            await Task.Delay(TimeSpan.FromSeconds(configuration.GetValue("Monitoring:PollingSeconds", 15)), ct);
        }
    }
}
