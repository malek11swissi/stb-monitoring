using StbMonitoring.Application.Contracts;
namespace StbMonitoring.Application.Services;

public sealed partial class OperationsService
{
    /// <summary>
    /// Modifie la qualification métier d'un incident. Un changement de priorité
    /// recalcule les échéances avec la politique SLA correspondante et laisse
    /// une trace avant/après dans l'historique.
    /// </summary>
    public async Task<IncidentResponse> UpdateIncidentAsync(Guid id, UpdateIncidentRequest request,
        Guid actor, CancellationToken cancellationToken)
    {
        var incident = await IncidentRequired(id, cancellationToken);
        var oldValue = $"{incident.Title}|{incident.Category}|{incident.Priority}";
        var sla = await store.GetSlaForAsync(request.Priority, cancellationToken);

        incident.UpdateDetails(request.Title, request.Description, request.Category, request.Priority,
            request.SystemId, request.EndpointId, sla?.Id, sla?.ResponseTimeMinutes ?? 60,
            sla?.ResolutionTimeMinutes ?? 480);
        store.AddHistory(new(incident.Id, actor, "INCIDENT_UPDATED", oldValue,
            $"{incident.Title}|{incident.Category}|{incident.Priority}"));
        await store.SaveChangesAsync(cancellationToken);
        return await IncidentResponseRequired(id, cancellationToken);
    }
}
