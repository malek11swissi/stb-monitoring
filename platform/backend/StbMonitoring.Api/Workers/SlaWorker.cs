using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Api.Workers;
/// <summary>
/// Réévalue régulièrement les échéances des incidents ouverts afin de faire
/// évoluer leur SLA de OnTrack vers AtRisk ou Breached sans action utilisateur.
/// </summary>
public sealed class SlaWorker(IServiceScopeFactory scopes,ILogger<SlaWorker> logger):BackgroundService
{protected override async Task ExecuteAsync(CancellationToken ct){await Task.Delay(TimeSpan.FromSeconds(10),ct);while(!ct.IsCancellationRequested){try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<IOperationsService>().RefreshSlasAsync(ct);}catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Erreur du worker SLA.");}await Task.Delay(TimeSpan.FromSeconds(30),ct);}}}
