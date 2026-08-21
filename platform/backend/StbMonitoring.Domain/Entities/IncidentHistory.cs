namespace StbMonitoring.Domain.Entities;
/// <summary>Historique immuable des transitions et modifications d'un incident.</summary>
public sealed class IncidentHistory
{
    private IncidentHistory() { }
    public IncidentHistory(Guid incidentId, Guid? userId, string action, string? oldValue = null, string? newValue = null, string? details = null)
    { Id = Guid.NewGuid(); IncidentId = incidentId; UserId = userId; Action = action; OldValue = oldValue; NewValue = newValue; Details = details; }
    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Details { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
