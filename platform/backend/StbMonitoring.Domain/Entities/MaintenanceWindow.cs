namespace StbMonitoring.Domain.Entities;
/// <summary>Période planifiée qui conserve les contrôles mais peut suspendre les alertes.</summary>

public sealed class MaintenanceWindow
{
    private MaintenanceWindow() { }
    public MaintenanceWindow(string title, string? description, Guid? systemId, Guid? endpointId, DateTime startsAt, DateTime endsAt, bool suppressAlerts, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Le titre est obligatoire.");
        if (endsAt <= startsAt) throw new ArgumentException("La fin doit être postérieure au début.");
        if (systemId is null && endpointId is null) throw new ArgumentException("Un système ou un endpoint doit être ciblé.");
        Id = Guid.NewGuid(); Title = title.Trim(); Description = description?.Trim(); SystemId = systemId; EndpointId = endpointId;
        StartsAt = startsAt.ToUniversalTime(); EndsAt = endsAt.ToUniversalTime(); SuppressAlerts = suppressAlerts; CreatedByUserId = createdBy;
    }
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? SystemId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public bool SuppressAlerts { get; private set; }
    public bool IsCancelled { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public void Update(string title, string? description, Guid? systemId, Guid? endpointId, DateTime startsAt, DateTime endsAt, bool suppressAlerts) { if (string.IsNullOrWhiteSpace(title) || endsAt <= startsAt || (systemId is null && endpointId is null)) throw new ArgumentException("Fenêtre de maintenance invalide."); Title=title.Trim();Description=description?.Trim();SystemId=systemId;EndpointId=endpointId;StartsAt=startsAt.ToUniversalTime();EndsAt=endsAt.ToUniversalTime();SuppressAlerts=suppressAlerts; }
    public void Cancel() => IsCancelled = true;
}
