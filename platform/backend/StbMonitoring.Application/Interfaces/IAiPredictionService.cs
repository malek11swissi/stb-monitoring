using StbMonitoring.Application.Contracts;
namespace StbMonitoring.Application.Interfaces;

public interface IAiPredictionService
{
    Task<SystemRiskPrediction> PredictSystemRiskAsync(Guid systemId,CancellationToken ct);
}
