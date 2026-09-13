using StbMonitoring.Application.Contracts;
namespace StbMonitoring.Application.Interfaces;

public interface IAiPredictionService
{
    Task<SystemRiskPrediction> PredictSystemRiskAsync(Guid systemId,CancellationToken ct);
    Task<IncidentResolutionRecommendation> RecommendResolutionAsync(Guid incidentId,Guid technicianId,CancellationToken ct);
    Task<TechnicianAssignmentRecommendation> RecommendTechnicianAsync(Guid incidentId,CancellationToken ct);
}
