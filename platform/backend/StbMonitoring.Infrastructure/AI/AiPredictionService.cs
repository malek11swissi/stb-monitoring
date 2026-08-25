using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Infrastructure.AI;

/// <summary>
/// Joint les données System, Endpoint, CheckResult, Alert, Incident et Maintenance,
/// puis délègue uniquement le calcul prédictif au microservice Flask.
/// </summary>
public sealed class AiPredictionService(HttpClient http,MonitoringDbContext db,IOptions<AiOptions> options,ILogger<AiPredictionService> logger):IAiPredictionService
{
    private readonly AiOptions settings=options.Value;

    public async Task<SystemRiskPrediction> PredictSystemRiskAsync(Guid systemId,CancellationToken ct)
    {
        var system=await db.Systems.AsNoTracking().Include(x=>x.Endpoints).SingleOrDefaultAsync(x=>x.Id==systemId,ct)
            ??throw new KeyNotFoundException("Système introuvable.");
        var active=system.Endpoints.Where(x=>x.IsActive).ToArray();var since=DateTime.UtcNow.AddHours(-Math.Clamp(settings.HistoryHours,1,720));
        var endpointIds=active.Select(x=>x.Id).ToArray();
        var history=await db.CheckResults.AsNoTracking().Where(x=>endpointIds.Contains(x.EndpointId)&&x.StartedAt>=since)
            .OrderByDescending(x=>x.StartedAt).ToArrayAsync(ct);
        var alertQuery=db.Alerts.AsNoTracking().Where(x=>x.SystemId==systemId&&x.Status!=AlertStatus.Resolved&&x.Status!=AlertStatus.Closed);
        var activeAlerts=await alertQuery.CountAsync(ct);var criticalAlerts=await alertQuery.CountAsync(x=>x.Severity==AlertSeverity.Critical,ct);
        var closed=new[]{IncidentStatus.Resolved,IncidentStatus.Closed,IncidentStatus.Cancelled};
        var incidentQuery=db.Incidents.AsNoTracking().Where(x=>x.SystemId==systemId&&!x.IsArchived&&!closed.Contains(x.Status));
        var incidents=await incidentQuery.CountAsync(ct);var highPriority=await incidentQuery.CountAsync(x=>x.Priority==IncidentPriority.P1Critical||x.Priority==IncidentPriority.P2High,ct);
        var now=DateTime.UtcNow;var maintenance=await db.MaintenanceWindows.AsNoTracking().AnyAsync(x=>!x.IsCancelled&&x.StartsAt<=now&&x.EndsAt>=now&&(x.SystemId==systemId||(x.EndpointId.HasValue&&endpointIds.Contains(x.EndpointId.Value))),ct);
        var endpoints=active.Select(endpoint=>new EndpointRiskInput(endpoint.Id,endpoint.Name,endpoint.IsCritical,endpoint.Status.ToString(),endpoint.DegradedThresholdMs,endpoint.DownThresholdMs,
            history.Where(x=>x.EndpointId==endpoint.Id).Take(Math.Clamp(settings.MaxSamplesPerEndpoint,10,500)).OrderBy(x=>x.StartedAt)
                .Select(x=>new CheckSampleInput(x.StartedAt,x.DurationMs,x.Status.ToString(),x.Success,x.HttpStatusCode,x.ErrorType)).ToArray())).ToArray();
        var request=new SystemRiskRequest(system.Id,system.Code,system.Name,system.Environment.ToString(),system.Criticality.ToString(),system.Status.ToString(),activeAlerts,criticalAlerts,incidents,highPriority,maintenance,endpoints);
        try
        {
            using var response=await http.PostAsJsonAsync("api/v1/predictions/system-risk",request,ct);
            if(!response.IsSuccessStatusCode){var body=await response.Content.ReadAsStringAsync(ct);logger.LogWarning("Service IA HTTP {Status}: {Body}",(int)response.StatusCode,body);return Unavailable(systemId,$"Service IA en erreur HTTP {(int)response.StatusCode}.");}
            return await response.Content.ReadFromJsonAsync<SystemRiskPrediction>(cancellationToken:ct)??Unavailable(systemId,"Réponse IA vide.");
        }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){logger.LogWarning("Timeout du service IA pour {SystemId}",systemId);return Unavailable(systemId,"Le service IA n'a pas répondu dans le délai configuré.");}
        catch(HttpRequestException ex){logger.LogWarning(ex,"Service IA indisponible pour {SystemId}",systemId);return Unavailable(systemId,"Microservice IA temporairement indisponible.");}
    }

    public async Task<IncidentResolutionRecommendation> RecommendResolutionAsync(Guid incidentId,Guid technicianId,CancellationToken ct)
    {
        var incident=await db.Incidents.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==incidentId,ct)??throw new KeyNotFoundException("Incident introuvable.");
        if(incident.AssignedToUserId!=technicianId)throw new UnauthorizedAccessException("Seul le technicien affecté peut demander une recommandation.");
        var systemNames=await db.Systems.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
        var history=await db.Incidents.AsNoTracking().Where(x=>x.Id!=incidentId&&(x.Status==IncidentStatus.Resolved||x.Status==IncidentStatus.Closed)&&x.CorrectiveAction!=null)
            .OrderByDescending(x=>x.ResolvedAt).Take(300).ToArrayAsync(ct);
        string? Name(Guid? id)=>id.HasValue&&systemNames.TryGetValue(id.Value,out var name)?name:null;
        var request=new IncidentRecommendationRequest(
            new(incident.Id,incident.Title,incident.Description,incident.Category.ToString(),incident.Priority.ToString(),incident.SystemId,Name(incident.SystemId)),
            history.Select(x=>new ResolvedIncidentInput(x.Id,x.IncidentNumber,x.Title,x.Description,x.Category.ToString(),x.Priority.ToString(),x.SystemId,Name(x.SystemId),x.RootCause,x.CorrectiveAction!,x.PreventiveAction,x.ResolutionSummary)).ToArray());
        try{
            using var response=await http.PostAsJsonAsync("api/v1/predictions/incident-resolution",request,ct);
            if(!response.IsSuccessStatusCode)return UnavailableRecommendation($"Service IA en erreur HTTP {(int)response.StatusCode}.");
            return await response.Content.ReadFromJsonAsync<IncidentResolutionRecommendation>(cancellationToken:ct)??UnavailableRecommendation("Réponse IA vide.");
        }catch(Exception ex)when(ex is HttpRequestException or TaskCanceledException){logger.LogWarning(ex,"Recommandation IA indisponible pour {IncidentId}",incidentId);return UnavailableRecommendation("Microservice IA temporairement indisponible.");}
    }

    private static SystemRiskPrediction Unavailable(Guid id,string message)=>new(false,id,"system-risk-random-forest","unavailable","unavailable",0,0,0,0,0,"Unavailable",0,null,[],[],DateTime.UtcNow,message);
    private static IncidentResolutionRecommendation UnavailableRecommendation(string message)=>new(false,"tfidf-cosine-retrieval","unavailable",0,[],message);
}
