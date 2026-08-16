namespace StbMonitoring.Domain.Entities;
public sealed class AlertOccurrence
{
    private AlertOccurrence() { }
    public AlertOccurrence(Guid alertId, Guid checkResultId, MonitoringStatus status, string? errorType, string? errorMessage)
    { Id = Guid.NewGuid(); AlertId = alertId; CheckResultId = checkResultId; Status = status; ErrorType = errorType; ErrorMessage = errorMessage; }
    public Guid Id { get; private set; }
    public Guid AlertId { get; private set; }
    public Guid CheckResultId { get; private set; }
    public MonitoringStatus Status { get; private set; }
    public string? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime DetectedAt { get; private set; } = DateTime.UtcNow;
}
