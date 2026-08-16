namespace StbMonitoring.Domain.Entities;

public sealed class AlertRule
{
    private AlertRule() { }

    public AlertRule(string name, string description, AlertEventType eventType, AlertSeverity severity,
        int failures, int deduplicationMinutes, bool autoResolve, bool notifyInApp,
        Guid? systemId = null, Guid? endpointId = null)
    {
        Id = Guid.NewGuid();
        Update(name, description, eventType, severity, failures, deduplicationMinutes,
            autoResolve, notifyInApp, systemId, endpointId);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AlertEventType EventType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public int DeduplicationMinutes { get; private set; }
    public bool AutoResolve { get; private set; }
    public bool NotifyInApp { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? SystemId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public void Update(string name, string description, AlertEventType eventType, AlertSeverity severity,
        int failures, int deduplicationMinutes, bool autoResolve, bool notifyInApp,
        Guid? systemId, Guid? endpointId)
    {
        Name = name.Trim(); Description = description.Trim(); EventType = eventType; Severity = severity;
        ConsecutiveFailures = Math.Max(1, failures); DeduplicationMinutes = Math.Max(1, deduplicationMinutes);
        AutoResolve = autoResolve; NotifyInApp = notifyInApp; SystemId = systemId; EndpointId = endpointId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool value) { IsActive = value; UpdatedAt = DateTime.UtcNow; }
}
