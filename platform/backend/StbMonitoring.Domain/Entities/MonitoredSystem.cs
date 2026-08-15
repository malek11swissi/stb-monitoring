namespace StbMonitoring.Domain.Entities;
public sealed class MonitoredSystem
{
    private MonitoredSystem() { }
    public MonitoredSystem(string code,string name,string description,SystemEnvironment environment,SystemCriticality criticality,string? owner)
    {Id=Guid.NewGuid();Code=code.Trim().ToUpperInvariant();Name=name.Trim();Description=description.Trim();Environment=environment;Criticality=criticality;Owner=owner?.Trim();}
    public Guid Id{get;private set;} public string Code{get;private set;}=string.Empty; public string Name{get;private set;}=string.Empty; public string Description{get;private set;}=string.Empty;
    public SystemEnvironment Environment{get;private set;} public SystemCriticality Criticality{get;private set;} public string? Owner{get;private set;} public bool MonitoringEnabled{get;private set;}=true; public bool IsArchived{get;private set;}
    public MonitoringStatus Status{get;private set;}=MonitoringStatus.Unknown; public DateTime CreatedAt{get;private set;}=DateTime.UtcNow; public DateTime UpdatedAt{get;private set;}=DateTime.UtcNow; public DateTime? ArchivedAt{get;private set;} public DateTime? LastCheckedAt{get;private set;}
    public ICollection<MonitoringEndpoint> Endpoints{get;}=new List<MonitoringEndpoint>();
    public void Update(string code,string name,string description,SystemEnvironment environment,SystemCriticality criticality,string? owner){Code=code.Trim().ToUpperInvariant();Name=name.Trim();Description=description.Trim();Environment=environment;Criticality=criticality;Owner=owner?.Trim();UpdatedAt=DateTime.UtcNow;}
    public void SetMonitoring(bool enabled){MonitoringEnabled=enabled;UpdatedAt=DateTime.UtcNow;if(!enabled)Status=MonitoringStatus.Unknown;}
    public void Archive(){IsArchived=true;ArchivedAt=DateTime.UtcNow;MonitoringEnabled=false;Status=MonitoringStatus.Unknown;UpdatedAt=DateTime.UtcNow;} public void Restore(){IsArchived=false;ArchivedAt=null;UpdatedAt=DateTime.UtcNow;}
    public void RecalculateStatus(){var active=Endpoints.Where(x=>x.IsActive).ToArray();LastCheckedAt=active.Select(x=>x.LastCheckedAt).Max();if(active.Length==0||active.All(x=>!x.LastCheckedAt.HasValue)){Status=MonitoringStatus.Unknown;return;}if(active.Any(x=>x.IsCritical&&x.Status==MonitoringStatus.Down)){Status=MonitoringStatus.Down;return;}if(active.Any(x=>x.Status is MonitoringStatus.Down or MonitoringStatus.Degraded)){Status=MonitoringStatus.Degraded;return;}Status=active.All(x=>x.Status==MonitoringStatus.Up)?MonitoringStatus.Up:MonitoringStatus.Unknown;}
}
