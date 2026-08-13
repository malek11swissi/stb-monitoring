namespace StbMonitoring.Domain.Entities;
public sealed class AuditLog
{
    private AuditLog() { }
    public AuditLog(Guid? userId, string action, string entityName, Guid? entityId, string? details, string? ipAddress)
    { Id = Guid.NewGuid(); UserId = userId; Action = action; EntityName = entityName; EntityId = entityId; Details = details; IpAddress = ipAddress; }
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
