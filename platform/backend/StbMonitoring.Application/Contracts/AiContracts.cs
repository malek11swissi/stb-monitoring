namespace StbMonitoring.Application.Contracts;

/// <summary>Données brutes envoyées au microservice IA pour un SI et ses endpoints.</summary>
public sealed record SystemRiskRequest(
    Guid SystemId,string SystemCode,string SystemName,string Environment,string Criticality,string CurrentStatus,
    int ActiveAlertCount,int CriticalAlertCount,int OpenIncidentCount,int HighPriorityIncidentCount,bool MaintenanceActive,
    IReadOnlyCollection<EndpointRiskInput> Endpoints);

public sealed record EndpointRiskInput(
    Guid EndpointId,string Name,bool IsCritical,string CurrentStatus,int DegradedThresholdMs,int DownThresholdMs,
    IReadOnlyCollection<CheckSampleInput> Samples);

public sealed record CheckSampleInput(DateTime Timestamp,long DurationMs,string Status,bool Success,int? HttpStatusCode,string? ErrorType);

/// <summary>Prédiction explicable retournée par Flask. Available=false signifie que l'IA est indisponible, sans bloquer le monitoring.</summary>
public sealed record SystemRiskPrediction(
    bool Available,Guid SystemId,string ModelName,string ModelVersion,string ModelSource,int SampleCount,double Risk15Minutes,double Risk30Minutes,
    double Risk60Minutes,int RiskScore,string RiskLevel,double Confidence,double? EstimatedTimeToDownMinutes,
    IReadOnlyCollection<RiskFactor> Factors,IReadOnlyCollection<RiskyEndpoint> MostRiskyEndpoints,DateTime GeneratedAt,string? Message=null);

public sealed record RiskFactor(string Code,string Label,double Contribution);
public sealed record RiskyEndpoint(Guid EndpointId,string Name,int RiskScore,string RiskLevel);
