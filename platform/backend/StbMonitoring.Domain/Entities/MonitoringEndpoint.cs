namespace StbMonitoring.Domain.Entities;
/// <summary>Point de contrôle HTTP, API JSON ou TLS et sa planification.</summary>
public sealed class MonitoringEndpoint
{
    private MonitoringEndpoint() { }
    public MonitoringEndpoint(Guid systemId,
    string name,
    string? url,
    CheckType checkType,
    string httpMethod,
    int expectedStatusCode,
    int timeoutSeconds,
    int intervalSeconds,
    int degradedThresholdMs,
    int downThresholdMs,
    bool isCritical,
    string? expectedJsonProperty,
    string? expectedJsonValue,
    DatabaseEngine? databaseEngine = null, string? databaseHost = null, int? databasePort = null,
    string? databaseName = null, string? databaseGroup = null, DatabaseRole? declaredRole = null)
    {Id=Guid.NewGuid();SystemId=systemId;Update(name,url,checkType,httpMethod,expectedStatusCode,timeoutSeconds,intervalSeconds,degradedThresholdMs,downThresholdMs,isCritical,expectedJsonProperty,expectedJsonValue,databaseEngine,databaseHost,databasePort,databaseName,databaseGroup,declaredRole);NextCheckAt=DateTime.UtcNow;}
    public Guid Id{get;private set;} public Guid SystemId{get;private set;} public MonitoredSystem System{get;private set;}=null!; public string Name{get;private set;}=string.Empty; public string Url{get;private set;}=string.Empty; public CheckType CheckType{get;private set;} public string HttpMethod{get;private set;}="GET";
    public int ExpectedStatusCode{get;private set;}=200; public int TimeoutSeconds{get;private set;}=10; public int IntervalSeconds{get;private set;}=60; public int DegradedThresholdMs{get;private set;}=1500; public int DownThresholdMs{get;private set;}=3000; public bool IsCritical{get;private set;}=true; public bool IsActive{get;private set;}=true;
    public string? ExpectedJsonProperty{get;private set;} public string? ExpectedJsonValue{get;private set;} public MonitoringStatus Status{get;private set;}=MonitoringStatus.Unknown; public DateTime? LastCheckedAt{get;private set;} public DateTime NextCheckAt{get;private set;} public long? LastDurationMs{get;private set;} public string? LastError{get;private set;} public ICollection<CheckResult> Results{get;}=new List<CheckResult>();
    public DatabaseEngine? DatabaseEngine { get; private set; }
    public string? DatabaseHost { get; private set; }
    public int? DatabasePort { get; private set; }
    public string? DatabaseName { get; private set; }
    public string? DatabaseGroup { get; private set; }
    public DatabaseRole? DeclaredRole { get; private set; }
    public void Update(string name,string? url,CheckType checkType,string httpMethod,int expectedStatusCode,int timeoutSeconds,int intervalSeconds,int degradedThresholdMs,int downThresholdMs,bool isCritical,string? expectedJsonProperty,string? expectedJsonValue,DatabaseEngine? databaseEngine=null,string? databaseHost=null,int? databasePort=null,string? databaseName=null,string? databaseGroup=null,DatabaseRole? declaredRole=null)
    {
        if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Le nom de l'endpoint est obligatoire.");
        if(!Enum.IsDefined(checkType))throw new ArgumentException("Type de contrôle invalide.");
        if(timeoutSeconds is <1 or >300)throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        if(intervalSeconds is <10 or >86400)throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
        if(degradedThresholdMs<1||downThresholdMs<degradedThresholdMs)throw new ArgumentException("Les seuils de performance sont invalides.");
        if(checkType==CheckType.Database)
        {
            if(databaseEngine is null||!Enum.IsDefined(databaseEngine.Value))throw new ArgumentException("Moteur de base de données invalide.");
            if(string.IsNullOrWhiteSpace(databaseHost)||databaseHost.Length>253||databaseHost.Contains('/')||databaseHost.Contains(':')||databaseHost.Any(char.IsWhiteSpace))throw new ArgumentException("Hôte de base de données invalide.");
            if(databasePort is <1 or >65535 or null)throw new ArgumentException("Port de base de données invalide.");
            if(string.IsNullOrWhiteSpace(databaseGroup)||databaseGroup.Length>100)throw new ArgumentException("Groupe de réplication obligatoire (100 caractères max).");
            if(declaredRole is null||!Enum.IsDefined(declaredRole.Value))throw new ArgumentException("Rôle déclaré invalide.");
            if(databaseName?.Length>120)throw new ArgumentException("Nom de base trop long.");
            DatabaseEngine=databaseEngine;DatabaseHost=databaseHost.Trim();DatabasePort=databasePort;DatabaseName=databaseName?.Trim();DatabaseGroup=databaseGroup.Trim();DeclaredRole=declaredRole;
            Url=$"{databaseEngine.ToString()!.ToLowerInvariant()}://{DatabaseHost}:{DatabasePort}";
            HttpMethod="GET";ExpectedStatusCode=200;ExpectedJsonProperty=null;ExpectedJsonValue=null;
        }
        else
        {
            if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme is not("http"or"https"))throw new ArgumentException("URL HTTP/HTTPS invalide.");
            var method=httpMethod.Trim().ToUpperInvariant();if(method is not("GET"or"HEAD"or"POST"))throw new ArgumentException("Méthode HTTP non supportée.");
            if(expectedStatusCode is <100 or >599)throw new ArgumentOutOfRangeException(nameof(expectedStatusCode));
            Url=url.Trim();HttpMethod=method;ExpectedStatusCode=expectedStatusCode;ExpectedJsonProperty=expectedJsonProperty?.Trim();ExpectedJsonValue=expectedJsonValue?.Trim();
            DatabaseEngine=null;DatabaseHost=null;DatabasePort=null;DatabaseName=null;DatabaseGroup=null;DeclaredRole=null;
        }
        Name=name.Trim();CheckType=checkType;TimeoutSeconds=timeoutSeconds;IntervalSeconds=intervalSeconds;DegradedThresholdMs=degradedThresholdMs;DownThresholdMs=downThresholdMs;IsCritical=isCritical;
    }
    public void SetActive(bool active){IsActive=active;if(!active)Status=MonitoringStatus.Unknown;NextCheckAt=DateTime.UtcNow;}
    public void Record(MonitoringStatus status,long durationMs,string? error){Status=status;LastDurationMs=durationMs;LastError=error;LastCheckedAt=DateTime.UtcNow;NextCheckAt=DateTime.UtcNow.AddSeconds(IntervalSeconds);}
}
