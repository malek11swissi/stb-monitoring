using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Application.Services;
/// <summary>
/// Moteur opérationnel de la plateforme. Il transforme les résultats techniques
/// en alertes dédupliquées, puis permet leur qualification en incidents suivis
/// par SLA, historique, notifications et escalade.
/// </summary>
public sealed partial class OperationsService(IOperationsStore store,IIncidentNotificationDispatcher incidentNotifications) : IOperationsService
{
    /// <summary>
    /// Applique les règles d'alerte à un résultat de contrôle. Une maintenance
    /// peut supprimer l'alerte sans arrêter le contrôle ni son historisation.
    /// </summary>
    public async Task EvaluateAsync(CheckResult result, CancellationToken ct)
    {
        var endpoint = await store.GetEndpointForAlertAsync(result.EndpointId, ct);
        if (endpoint is null) return;
        if (result.Status != MonitoringStatus.Up && await store.IsAlertSuppressedAsync(result.SystemId, result.EndpointId, result.CompletedAt, ct)) return;
        if (result.Status == MonitoringStatus.Up)
        {
            // Le retour à UP résout seulement les alertes dont la règle autorise
            // la résolution automatique. La clôture reste une action humaine.
            var rulesById = (await store.GetRulesAsync(ct)).ToDictionary(x => x.Id);
            foreach (var alert in await store.GetActiveAlertsForEndpointAsync(result.EndpointId, ct))
                if (alert.RuleId.HasValue && rulesById.TryGetValue(alert.RuleId.Value, out var rule) && rule.AutoResolve)
                    alert.Resolve();
            await store.SaveChangesAsync(ct);
            return;
        }
        var type = Event(result);
        var rules = (await store.GetRulesAsync(ct)).Where(x => x.IsActive && x.EventType == type && (!x.SystemId.HasValue || x.SystemId == result.SystemId) && (!x.EndpointId.HasValue || x.EndpointId == result.EndpointId)).ToArray();
        foreach (var rule in rules)
        {
            // On attend N échecs consécutifs afin d'éviter une alerte pour une
            // indisponibilité très brève ou un incident réseau isolé.
            if (await store.ConsecutiveFailuresAsync(result.EndpointId, rule.ConsecutiveFailures, ct) < rule.ConsecutiveFailures) continue;
            var key = $"{result.SystemId}:{result.EndpointId}:{type}:{rule.Id}";
            // La clé et la fenêtre temporelle réutilisent une alerte existante
            // au lieu de créer une tempête d'alertes identiques.
            var alert = await store.GetDeduplicatedAlertAsync(key, DateTime.UtcNow.AddMinutes(-rule.DeduplicationMinutes), ct);
            if (alert is null)
            {
                var number = $"ALT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                alert = new Alert(number, rule.Id, result.SystemId, result.EndpointId, result.Id, type, $"{endpoint.System.Name} — {endpoint.Name} {result.Status}", result.ErrorMessage ?? $"Contrôle {result.Status}", Severity(rule, endpoint.System.Criticality, result.Status), key, result.ErrorMessage);
                store.AddAlert(alert);
                await store.SaveChangesAsync(ct);
                if (rule.NotifyInApp) await NotifyAdmins("ALERT_CREATED", alert.Title, alert.Description, alert.Severity, "Alert", alert.Id, $"/alerts/{alert.Id}", ct);
            }
            else alert.Occur(result.Id, result.ErrorMessage);
            // L'occurrence conserve le lien exact vers chaque contrôle ayant
            // contribué à l'alerte et sert de preuve dans sa chronologie.
            store.AddOccurrence(new AlertOccurrence(alert.Id, result.Id, result.Status, result.ErrorType, result.ErrorMessage));
            await store.SaveChangesAsync(ct);
        }
    }
    private static AlertEventType Event(CheckResult r) => r.ErrorType switch { "TIMEOUT" => AlertEventType.Timeout, "TLS_EXPIRING" => AlertEventType.TlsExpiring, "TLS_EXPIRED" => AlertEventType.TlsExpired, "TLS_NAME_MISMATCH" or "TLS_UNTRUSTED" or "TLS_NOT_YET_VALID" or "TLS_HANDSHAKE" or "TLS_CONNECTION" => AlertEventType.TlsInvalid, "VALIDATION" => AlertEventType.ValidationFailed, _ => r.Status == MonitoringStatus.Degraded ? AlertEventType.EndpointDegraded : AlertEventType.EndpointDown }; private static AlertSeverity Severity(AlertRule r, SystemCriticality c, MonitoringStatus s) => s == MonitoringStatus.Down && c == SystemCriticality.Critical ? AlertSeverity.Critical : s == MonitoringStatus.Down && c >= SystemCriticality.High ? AlertSeverity.Major : r.Severity;
    public Task<IReadOnlyCollection<AlertResponse>> AlertsAsync(CancellationToken ct) => store.GetAlertsAsync(ct); public async Task<AlertDetailResponse?> AlertAsync(Guid id, CancellationToken ct) { var a = (await store.GetAlertsAsync(ct)).SingleOrDefault(x => x.Id == id); return a is null ? null : new(a, await store.GetOccurrencesAsync(id, ct)); }
    public async Task AcknowledgeAsync(Guid id, Guid userId, CancellationToken ct) { (await AlertRequired(id, ct)).Acknowledge(userId); await store.SaveChangesAsync(ct); }
    public async Task ResolveAlertAsync(Guid id, CancellationToken ct) { (await AlertRequired(id, ct)).Resolve(); await store.SaveChangesAsync(ct); }
    public async Task CloseAlertAsync(Guid id, Guid userId, CancellationToken ct) { (await AlertRequired(id, ct)).Close(userId); await store.SaveChangesAsync(ct); }
    public async Task<IReadOnlyCollection<AlertRuleResponse>> RulesAsync(CancellationToken ct) => (await store.GetRulesAsync(ct)).Select(Map).ToArray(); public async Task<AlertRuleResponse> CreateRuleAsync(AlertRuleRequest r, CancellationToken ct) { var x = new AlertRule(r.Name, r.Description, r.EventType, r.Severity, r.ConsecutiveFailures, r.DeduplicationMinutes, r.AutoResolve, r.NotifyInApp, r.SystemId, r.EndpointId); store.AddRule(x); await store.SaveChangesAsync(ct); return Map(x); }
    public async Task<AlertRuleResponse> UpdateRuleAsync(Guid id, AlertRuleRequest r, CancellationToken ct) { var x = await RuleRequired(id, ct); x.Update(r.Name, r.Description, r.EventType, r.Severity, r.ConsecutiveFailures, r.DeduplicationMinutes, r.AutoResolve, r.NotifyInApp, r.SystemId, r.EndpointId); await store.SaveChangesAsync(ct); return Map(x); }
    public async Task SetRuleActiveAsync(Guid id, bool value, CancellationToken ct) { (await RuleRequired(id, ct)).SetActive(value); await store.SaveChangesAsync(ct); }
    public async Task DeleteRuleAsync(Guid id, CancellationToken ct) { var x = await RuleRequired(id, ct); store.RemoveRule(x); await store.SaveChangesAsync(ct); }
    public Task<IReadOnlyCollection<IncidentResponse>> IncidentsAsync(CancellationToken ct) => store.GetIncidentsAsync(ct); public Task<IncidentDetailResponse?> IncidentAsync(Guid id, CancellationToken ct) => store.GetIncidentDetailAsync(id, ct);
    /// <summary>
    /// Crée un incident ou rattache l'alerte à un incident récent du même SI.
    /// Cette corrélation évite plusieurs tickets lorsque plusieurs endpoints
    /// tombent à cause d'une même panne globale.
    /// </summary>
    public async Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest r, Guid actor, CancellationToken ct) { Alert? alert = null; if (r.AlertId.HasValue) { alert = await AlertRequired(r.AlertId.Value, ct); if (alert.IncidentId.HasValue) throw new InvalidOperationException("Cette alerte possède déjà un incident."); var correlated=await store.FindCorrelatedIncidentAsync(alert.SystemId,DateTime.UtcNow.AddMinutes(-30),ct);if(correlated is not null){alert.LinkIncident(correlated.Id);store.AddHistory(new(correlated.Id,actor,"ALERT_CORRELATED",details:alert.AlertNumber));await store.SaveChangesAsync(ct);return await IncidentResponseRequired(correlated.Id,ct);} } var priority = r.Priority;
    // La priorité choisit la politique SLA active et fixe immédiatement les échéances.
    var sla = await store.GetSlaForAsync(priority, ct); var x = new Incident($"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}", r.AlertId, r.SystemId ?? alert?.SystemId, r.EndpointId ?? alert?.EndpointId, r.Title, r.Description, r.Category, priority, actor, sla?.Id, sla?.ResponseTimeMinutes ?? 60, sla?.ResolutionTimeMinutes ?? 480); store.AddIncident(x); store.AddHistory(new IncidentHistory(x.Id, actor, "INCIDENT_CREATED", null, x.Status.ToString())); await store.SaveChangesAsync(ct); if (alert is not null) { alert.LinkIncident(x.Id); await store.SaveChangesAsync(ct); } await NotifyAdmins("INCIDENT_CREATED", x.IncidentNumber, x.Title, AlertSeverity.Major, "Incident", x.Id, $"/incidents/{x.Id}", ct); return await IncidentResponseRequired(x.Id, ct); }
    public async Task AssignAsync(Guid id, Guid userId, Guid actor, CancellationToken ct) { if (!await store.CanAssignIncidentToAsync(userId, ct)) throw new InvalidOperationException("Un incident ne peut être affecté qu'à un technicien actif."); var x = await IncidentRequired(id, ct); var old = x.Status.ToString(); x.Assign(userId, actor); store.AddHistory(new(x.Id, actor, "INCIDENT_ASSIGNED", old, x.Status.ToString(), userId.ToString())); store.AddNotification(new(userId, "INCIDENT_ASSIGNED", x.IncidentNumber, x.Title, AlertSeverity.Major, "Incident", x.Id, $"/incidents/{x.Id}")); await store.SaveChangesAsync(ct); await incidentNotifications.NotifyAssignmentAsync(x,ct); }
    public async Task StartAsync(Guid id, Guid actor, CancellationToken ct) => await Change(id, actor, "INCIDENT_STARTED", x => x.Start(), ct); public async Task PendingAsync(Guid id, Guid actor, CancellationToken ct) => await Change(id, actor, "INCIDENT_PENDING", x => x.Pending(), ct); public async Task ResolveIncidentAsync(Guid id, ResolveIncidentRequest r, Guid actor, CancellationToken ct) { var x = await IncidentRequired(id, ct);if(string.IsNullOrWhiteSpace(r.RootCause))throw new ArgumentException("La cause racine est obligatoire.");if((x.Priority is IncidentPriority.P1Critical or IncidentPriority.P2High)&&!await store.HasResolutionProofAsync(id,ct)&&string.IsNullOrWhiteSpace(r.ResolutionEvidence))throw new ArgumentException("Une preuve de résolution est obligatoire pour un incident P1 ou P2."); var old = x.Status.ToString(); x.Resolve(r.Summary, r.RootCause, r.CorrectiveAction, r.PreventiveAction, r.ResolutionEvidence); store.AddHistory(new(x.Id, actor, "INCIDENT_RESOLVED", old, x.Status.ToString(), r.Summary));foreach(var userId in await store.GetActiveUserIdsByRolesAsync([RoleNames.Supervisor,RoleNames.ManagerIt],ct))store.AddNotification(new(userId,"INCIDENT_RESOLVED",$"✓ {x.IncidentNumber} résolu",x.Title,AlertSeverity.Info,"Incident",x.Id,$"/incidents/{x.Id}")); await store.SaveChangesAsync(ct); }
    public async Task CloseIncidentAsync(Guid id, Guid actor, CancellationToken ct) => await Change(id, actor, "INCIDENT_CLOSED", x => x.Close(actor), ct); public async Task ReopenAsync(Guid id, Guid actor, CancellationToken ct) => await Change(id, actor, "INCIDENT_REOPENED", x => x.Reopen(), ct);
    public async Task CancelIncidentAsync(Guid id,string reason,Guid actor,CancellationToken ct)=>await Change(id,actor,"INCIDENT_CANCELLED",x=>x.Cancel(reason,actor),ct,reason);
    public async Task ArchiveIncidentAsync(Guid id,Guid actor,CancellationToken ct)=>await Change(id,actor,"INCIDENT_ARCHIVED",x=>x.Archive(actor),ct);
    public async Task RestoreIncidentAsync(Guid id,Guid actor,CancellationToken ct)=>await Change(id,actor,"INCIDENT_RESTORED",x=>x.Restore(),ct);
    public async Task DeleteIncidentAsync(Guid id,string reason,Guid actor,CancellationToken ct){if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Le motif de suppression est obligatoire.");var x=await IncidentRequired(id,ct);if(x.Status is not (IncidentStatus.Closed or IncidentStatus.Cancelled))throw new InvalidOperationException("Seul un incident clôturé ou annulé peut être supprimé définitivement.");foreach(var alert in await store.GetAlertsAsync(ct)){if(alert.IncidentId==x.Id)(await AlertRequired(alert.Id,ct)).UnlinkIncident();}store.RemoveIncident(x);await store.SaveChangesAsync(ct);}
    public async Task CommentAsync(Guid id, AddCommentRequest r, Guid actor, CancellationToken ct) { var incident=await IncidentRequired(id, ct); if(incident.IsArchived)throw new InvalidOperationException("Un incident archivé est en lecture seule."); store.AddComment(new(id, actor, r.Content, r.CommentType, r.IsInternal)); store.AddHistory(new(id, actor, "INCIDENT_COMMENT_ADDED", details: r.Content)); await store.SaveChangesAsync(ct); }
    public async Task<IReadOnlyCollection<SlaPolicyResponse>> SlasAsync(CancellationToken ct) => (await store.GetSlasAsync(ct)).Select(Map).ToArray(); public async Task<SlaPolicyResponse> CreateSlaAsync(SlaPolicyRequest r, CancellationToken ct) { await EnsureUniqueActiveSla(r.Priority, null, ct); var x = new SlaPolicy(r.Name, r.Description, r.Priority, r.ResponseTimeMinutes, r.ResolutionTimeMinutes, r.WarningPercentage); store.AddSla(x); await store.SaveChangesAsync(ct); return Map(x); }
    public async Task<SlaPolicyResponse> UpdateSlaAsync(Guid id, SlaPolicyRequest r, CancellationToken ct) { var x = await store.GetSlaAsync(id, ct) ?? throw new KeyNotFoundException("SLA introuvable."); if (x.IsActive) await EnsureUniqueActiveSla(r.Priority, id, ct); x.Update(r.Name, r.Description, r.Priority, r.ResponseTimeMinutes, r.ResolutionTimeMinutes, r.WarningPercentage); await store.SaveChangesAsync(ct); return Map(x); }
    public async Task SetSlaActiveAsync(Guid id, bool value, CancellationToken ct) { var x = await store.GetSlaAsync(id, ct) ?? throw new KeyNotFoundException("SLA introuvable."); if (value) await EnsureUniqueActiveSla(x.Priority, id, ct); x.SetActive(value); await store.SaveChangesAsync(ct); }
    /// <summary>Recalcule les SLA ouverts et notifie une seule fois au passage à Breached.</summary>
    public async Task RefreshSlasAsync(CancellationToken ct) { foreach (var x in await store.GetOpenIncidentsAsync(ct)) { var old = x.SlaStatus; x.RefreshSla(DateTime.UtcNow); if (old != SlaStatus.Breached && x.SlaStatus == SlaStatus.Breached) await NotifyAdmins("SLA_BREACHED", x.IncidentNumber, x.Title, AlertSeverity.Critical, "Incident", x.Id, $"/incidents/{x.Id}", ct); } await store.SaveChangesAsync(ct); }
    public async Task<IReadOnlyCollection<NotificationResponse>> NotificationsAsync(Guid userId, CancellationToken ct) => (await store.GetNotificationsAsync(userId, ct)).Select(Map).ToArray(); public async Task ReadNotificationAsync(Guid id, Guid userId, CancellationToken ct) { var x = await store.GetNotificationAsync(id, userId, ct) ?? throw new KeyNotFoundException("Notification introuvable."); x.Read(); await store.SaveChangesAsync(ct); }
    public async Task ReadAllAsync(Guid userId, CancellationToken ct) { foreach (var x in await store.GetNotificationsAsync(userId, ct)) x.Read(); await store.SaveChangesAsync(ct); }
    public Task<OperationsSummary> SummaryAsync(Guid userId, CancellationToken ct) => store.GetSummaryAsync(userId, ct);
    private async Task Change(Guid id, Guid actor, string action, Action<Incident> change, CancellationToken ct,string? details=null) { var x = await IncidentRequired(id, ct); var old = x.Status.ToString(); change(x); store.AddHistory(new(x.Id, actor, action, old, x.Status.ToString(),details)); await store.SaveChangesAsync(ct); }
    private async Task NotifyAdmins(string type, string title, string message, AlertSeverity severity, string entity, Guid id, string url, CancellationToken ct) { foreach (var user in await store.GetAdminUserIdsAsync(ct)) store.AddNotification(new(user, type, title, message, severity, entity, id, url)); await store.SaveChangesAsync(ct); }
    private async Task EnsureUniqueActiveSla(IncidentPriority priority, Guid? currentId, CancellationToken ct) { var active = await store.GetSlaForAsync(priority, ct); if (active is not null && active.Id != currentId) throw new InvalidOperationException($"Une politique SLA active existe déjà pour la priorité {priority}."); }
    private async Task<Alert> AlertRequired(Guid id, CancellationToken ct) => await store.GetAlertAsync(id, ct) ?? throw new KeyNotFoundException("Alerte introuvable."); private async Task<AlertRule> RuleRequired(Guid id, CancellationToken ct) => await store.GetRuleAsync(id, ct) ?? throw new KeyNotFoundException("Règle introuvable."); private async Task<Incident> IncidentRequired(Guid id, CancellationToken ct) => await store.GetIncidentAsync(id, ct) ?? throw new KeyNotFoundException("Incident introuvable."); private async Task<IncidentResponse> IncidentResponseRequired(Guid id, CancellationToken ct) => (await store.GetIncidentsAsync(ct)).Single(x => x.Id == id);
    private static AlertRuleResponse Map(AlertRule x) => new(x.Id, x.Name, x.Description, x.EventType, x.Severity, x.ConsecutiveFailures, x.DeduplicationMinutes, x.AutoResolve, x.NotifyInApp, x.IsActive, x.SystemId, x.EndpointId); private static SlaPolicyResponse Map(SlaPolicy x) => new(x.Id, x.Name, x.Description, x.Priority, x.ResponseTimeMinutes, x.ResolutionTimeMinutes, x.WarningPercentage, x.IsActive); private static NotificationResponse Map(Notification x) => new(x.Id, x.Type, x.Title, x.Message, x.Severity, x.EntityType, x.EntityId, x.ActionUrl, x.IsRead, x.CreatedAt);
}
