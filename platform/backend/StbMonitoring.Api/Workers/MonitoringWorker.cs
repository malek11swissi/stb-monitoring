using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Api.Workers;
public sealed class MonitoringWorker(IServiceScopeFactory scopes,IConfiguration configuration,ILogger<MonitoringWorker> logger):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken ct){await Task.Delay(TimeSpan.FromSeconds(5),ct);while(!ct.IsCancellationRequested){try{using var scope=scopes.CreateScope();var count=await scope.ServiceProvider.GetRequiredService<IMonitoringService>().ExecuteDueChecksAsync(ct);if(count>0)logger.LogInformation("{Count} contrôles de supervision exécutés.",count);}catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Erreur du worker de supervision.");}await Task.Delay(TimeSpan.FromSeconds(configuration.GetValue("Monitoring:PollingSeconds",15)),ct);}}
}
