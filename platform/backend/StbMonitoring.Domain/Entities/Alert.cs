namespace StbMonitoring.Domain.Entities;
/// <summary>Anomalie dédupliquée pouvant être acquittée, résolue puis clôturée.</summary>

public sealed class Alert
{
    private Alert() { }

    public Alert(string number, Guid? ruleId, Guid systemId, Guid endpointId, Guid checkResultId,
        AlertEventType type, string title, string description, AlertSeverity severity, string key, string? error)
    {
        Id = Guid.NewGuid(); AlertNumber = number; RuleId = ruleId; SystemId = systemId; EndpointId = endpointId;
        FirstCheckResultId = checkResultId; LastCheckResultId = checkResultId; Type = type; Title = title;
        Description = description; Severity = severity; DeduplicationKey = key; LastError = error;
        FirstDetectedAt = LastDetectedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string AlertNumber { get; private set; } = string.Empty;
    public Guid? RuleId { get; private set; }
    public Guid SystemId { get; private set; }
    public Guid EndpointId { get; private set; }
    public Guid FirstCheckResultId { get; private set; }
    public Guid LastCheckResultId { get; private set; }
    public Guid? IncidentId { get; private set; }
    public AlertEventType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; } = AlertStatus.Open;
    public string DeduplicationKey { get; private set; } = string.Empty;
    public int OccurrenceCount { get; private set; } = 1;
    public DateTime FirstDetectedAt { get; private set; }
    public DateTime LastDetectedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public void Occur(Guid resultId, string? error) { LastCheckResultId = resultId; LastDetectedAt = DateTime.UtcNow; OccurrenceCount++; LastError = error; UpdatedAt = DateTime.UtcNow; if (Status == AlertStatus.Resolved) Status = AlertStatus.Open; }
    public void Acknowledge(Guid userId) { if (Status is AlertStatus.Resolved or AlertStatus.Closed) throw new InvalidOperationException("Cette alerte n'est plus active."); Status = AlertStatus.Acknowledged; AcknowledgedAt = DateTime.UtcNow; AcknowledgedByUserId = userId; UpdatedAt = DateTime.UtcNow; }
    public void Resolve() { if (Status == AlertStatus.Closed) return; Status = AlertStatus.Resolved; ResolvedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void Close(Guid userId) { if (Status != AlertStatus.Resolved) throw new InvalidOperationException("Une alerte doit être résolue avant clôture."); Status = AlertStatus.Closed; ClosedAt = DateTime.UtcNow; ClosedByUserId = userId; UpdatedAt = DateTime.UtcNow; }
    public void LinkIncident(Guid id) { IncidentId = id; UpdatedAt = DateTime.UtcNow; }
}
