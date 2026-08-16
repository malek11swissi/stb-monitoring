namespace StbMonitoring.Domain.Entities;
public sealed class Notification
{
    private Notification() { }
    public Notification(Guid userId, string type, string title, string message, AlertSeverity severity,
        string? entityType, Guid? entityId, string? actionUrl)
    { Id = Guid.NewGuid(); UserId = userId; Type = type; Title = title; Message = message; Severity = severity; EntityType = entityType; EntityId = entityId; ActionUrl = actionUrl; }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public void Read() { IsRead = true; ReadAt = DateTime.UtcNow; }
}
