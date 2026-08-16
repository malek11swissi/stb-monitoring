namespace StbMonitoring.Domain.Entities;
public sealed class SlaPolicy
{
    private SlaPolicy() { }
    public SlaPolicy(string name, string description, IncidentPriority priority, int responseMinutes, int resolutionMinutes, int warningPercentage)
    { Id = Guid.NewGuid(); Update(name, description, priority, responseMinutes, resolutionMinutes, warningPercentage); }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IncidentPriority Priority { get; private set; }
    public int ResponseTimeMinutes { get; private set; }
    public int ResolutionTimeMinutes { get; private set; }
    public int WarningPercentage { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public void Update(string name, string description, IncidentPriority priority, int response, int resolution, int warning)
    { Name = name.Trim(); Description = description.Trim(); Priority = priority; ResponseTimeMinutes = Math.Max(1, response); ResolutionTimeMinutes = Math.Max(ResponseTimeMinutes, resolution); WarningPercentage = Math.Clamp(warning, 1, 90); UpdatedAt = DateTime.UtcNow; }
    public void SetActive(bool value) { IsActive = value; UpdatedAt = DateTime.UtcNow; }
}
