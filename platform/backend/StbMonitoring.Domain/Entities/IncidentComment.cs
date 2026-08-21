namespace StbMonitoring.Domain.Entities;
/// <summary>Échange fonctionnel ou technique ajouté à la chronologie d'un incident.</summary>
public sealed class IncidentComment
{
    private IncidentComment() { }
    public IncidentComment(Guid incidentId, Guid userId, string content, string type, bool isInternal)
    { if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Le commentaire est obligatoire."); Id = Guid.NewGuid(); IncidentId = incidentId; UserId = userId; Content = content.Trim(); CommentType = type; IsInternal = isInternal; }
    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string CommentType { get; private set; } = "Comment";
    public bool IsInternal { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
