namespace StbMonitoring.Domain.Entities;
/// <summary>Résultat immuable servant de preuve technique d'un contrôle.</summary>
public sealed class CheckResult
{
    private CheckResult() { }
    public CheckResult(Guid systemId,Guid endpointId,MonitoringStatus status,bool success,DateTime startedAt,DateTime completedAt,long durationMs,int? httpStatusCode,string? errorType,string? errorMessage,bool triggeredManually,Guid? triggeredByUserId,string? metadata)
    {Id=Guid.NewGuid();SystemId=systemId;EndpointId=endpointId;Status=status;Success=success;StartedAt=startedAt;CompletedAt=completedAt;DurationMs=durationMs;HttpStatusCode=httpStatusCode;ErrorType=errorType;ErrorMessage=errorMessage;TriggeredManually=triggeredManually;TriggeredByUserId=triggeredByUserId;Metadata=metadata;}
    public Guid Id{get;private set;} public Guid SystemId{get;private set;} public MonitoredSystem System{get;private set;}=null!; public Guid EndpointId{get;private set;} public MonitoringEndpoint Endpoint{get;private set;}=null!; public MonitoringStatus Status{get;private set;} public bool Success{get;private set;} public DateTime StartedAt{get;private set;} public DateTime CompletedAt{get;private set;} public long DurationMs{get;private set;} public int? HttpStatusCode{get;private set;} public string? ErrorType{get;private set;} public string? ErrorMessage{get;private set;} public bool TriggeredManually{get;private set;} public Guid? TriggeredByUserId{get;private set;} public string? Metadata{get;private set;}
}
