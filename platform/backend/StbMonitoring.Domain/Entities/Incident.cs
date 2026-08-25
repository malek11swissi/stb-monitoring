namespace StbMonitoring.Domain.Entities;
/// <summary>Ticket opérationnel portant affectation, cycle de vie, SLA et résolution.</summary>

public sealed class Incident
{
    private Incident() { }

    public Incident(string number, Guid? alertId, Guid? systemId, Guid? endpointId, string title,
        string description, IncidentCategory category, IncidentPriority priority, Guid creator,
        Guid? slaId, int responseMinutes, int resolutionMinutes)
    {
        Id = Guid.NewGuid(); IncidentNumber = number; AlertId = alertId; SystemId = systemId; EndpointId = endpointId;
        Title = title.Trim(); Description = description.Trim(); Category = category; Priority = priority;
        CreatedByUserId = creator; SlaPolicyId = slaId;
        ResponseDueAt = DateTime.UtcNow.AddMinutes(responseMinutes);
        ResolutionDueAt = DateTime.UtcNow.AddMinutes(resolutionMinutes);
    }

    public Guid Id { get; private set; }
    public string IncidentNumber { get; private set; } = string.Empty;
    public Guid? AlertId { get; private set; }
    public Guid? SystemId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IncidentCategory Category { get; private set; }
    public IncidentPriority Priority { get; private set; }
    public IncidentStatus Status { get; private set; } = IncidentStatus.New;
    public Guid? SlaPolicyId { get; private set; }
    public SlaStatus SlaStatus { get; private set; } = SlaStatus.OnTrack;
    public Guid? AssignedToUserId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ResponseDueAt { get; private set; }
    public DateTime? ResolutionDueAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public string? RootCause { get; private set; }
    public string? CorrectiveAction { get; private set; }
    public string? PreventiveAction { get; private set; }
    public string? ResolutionEvidence { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }
    public int ReopenCount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public void Assign(Guid userId, Guid by) { EnsureActive(); if (Status is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.Cancelled) throw new InvalidOperationException("Cet incident ne peut pas être affecté dans son état actuel."); AssignedToUserId = userId; AssignedByUserId = by; AssignedAt = DateTime.UtcNow; Status = IncidentStatus.Assigned; UpdatedAt = DateTime.UtcNow; }
    public void UpdateDetails(string title, string description, IncidentCategory category, IncidentPriority priority,
        Guid? systemId, Guid? endpointId, Guid? slaPolicyId, int responseMinutes, int resolutionMinutes)
    {
        EnsureActive();
        if (Status == IncidentStatus.Closed) throw new InvalidOperationException("Un incident clôturé doit être rouvert avant modification.");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Le titre est obligatoire.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("La description est obligatoire.");
        Title = title.Trim(); Description = description.Trim(); Category = category; SystemId = systemId; EndpointId = endpointId;
        if (Priority != priority) { Priority = priority; SlaPolicyId = slaPolicyId; ResponseDueAt = CreatedAt.AddMinutes(responseMinutes); ResolutionDueAt = CreatedAt.AddMinutes(resolutionMinutes); RefreshSla(DateTime.UtcNow); }
        UpdatedAt = DateTime.UtcNow;
    }
    public void Start() { EnsureActive(); if (AssignedToUserId is null) throw new InvalidOperationException("Affectez d'abord l'incident."); if (Status is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.Cancelled) throw new InvalidOperationException("Cet incident ne peut pas être démarré dans son état actuel."); Status = IncidentStatus.InProgress; FirstResponseAt ??= DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void Pending() { EnsureActive(); if (Status != IncidentStatus.InProgress) throw new InvalidOperationException("Seul un incident en cours peut être mis en attente."); Status = IncidentStatus.Pending; UpdatedAt = DateTime.UtcNow; }
    public void Resolve(string summary, string? rootCause, string? correctiveAction, string? preventiveAction, string? resolutionEvidence) { EnsureActive(); if (Status is not (IncidentStatus.InProgress or IncidentStatus.Pending or IncidentStatus.Reopened)) throw new InvalidOperationException("L'incident doit être pris en charge avant sa résolution."); if (string.IsNullOrWhiteSpace(summary)) throw new ArgumentException("Le résumé de résolution est obligatoire.");if(string.IsNullOrWhiteSpace(rootCause))throw new ArgumentException("La cause racine est obligatoire."); if (string.IsNullOrWhiteSpace(correctiveAction)) throw new ArgumentException("L'action corrective est obligatoire."); Status = IncidentStatus.Resolved; ResolutionSummary = summary.Trim(); RootCause = rootCause.Trim(); CorrectiveAction=correctiveAction.Trim(); PreventiveAction=preventiveAction?.Trim(); ResolutionEvidence=resolutionEvidence?.Trim(); ResolvedAt = DateTime.UtcNow; SlaStatus = ResolvedAt <= ResolutionDueAt ? SlaStatus.Met : SlaStatus.Breached; UpdatedAt = DateTime.UtcNow; }
    public void Resolve(string summary,string? rootCause)=>Resolve(summary,rootCause,"Action corrective documentée",null,null);
    public void Close(Guid by) { EnsureActive(); if (Status != IncidentStatus.Resolved) throw new InvalidOperationException("L'incident doit être résolu."); Status = IncidentStatus.Closed; ClosedByUserId = by; ClosedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void Reopen() { EnsureActive(); if (Status is not (IncidentStatus.Resolved or IncidentStatus.Closed)) throw new InvalidOperationException("Cet incident ne peut pas être rouvert."); Status = IncidentStatus.Reopened; ResolvedAt = null; ClosedAt = null; ReopenCount++; UpdatedAt = DateTime.UtcNow; }
    public void Cancel(string reason, Guid by) { EnsureActive(); if (Status is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.Cancelled) throw new InvalidOperationException("Cet incident ne peut pas être annulé dans son état actuel."); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Le motif d'annulation est obligatoire."); Status=IncidentStatus.Cancelled; CancellationReason=reason.Trim(); CancelledAt=DateTime.UtcNow; CancelledByUserId=by; UpdatedAt=DateTime.UtcNow; }
    public void Archive(Guid by) { if (IsArchived) throw new InvalidOperationException("Cet incident est déjà archivé."); if (Status is not (IncidentStatus.Closed or IncidentStatus.Cancelled)) throw new InvalidOperationException("Seul un incident clôturé ou annulé peut être archivé."); if (Status==IncidentStatus.Closed && (string.IsNullOrWhiteSpace(ResolutionSummary)||string.IsNullOrWhiteSpace(RootCause)||string.IsNullOrWhiteSpace(CorrectiveAction)||string.IsNullOrWhiteSpace(ResolutionEvidence))) throw new InvalidOperationException("La résolution doit contenir le résumé, la cause racine, l'action corrective et la preuve avant archivage."); IsArchived=true; ArchivedAt=DateTime.UtcNow; ArchivedByUserId=by; UpdatedAt=DateTime.UtcNow; }
    public void Restore() { if (!IsArchived) throw new InvalidOperationException("Cet incident n'est pas archivé."); IsArchived=false; ArchivedAt=null; ArchivedByUserId=null; UpdatedAt=DateTime.UtcNow; }
    private void EnsureActive() { if (IsArchived) throw new InvalidOperationException("Un incident archivé est en lecture seule. Restaurez-le d'abord."); }
    public void RefreshSla(DateTime now) { if (Status is IncidentStatus.Resolved or IncidentStatus.Closed) return; if (ResolutionDueAt <= now) SlaStatus = SlaStatus.Breached; else if (ResolutionDueAt.HasValue && now >= CreatedAt.AddTicks((ResolutionDueAt.Value - CreatedAt).Ticks * 75 / 100)) SlaStatus = SlaStatus.AtRisk; else SlaStatus = SlaStatus.OnTrack; }
}
