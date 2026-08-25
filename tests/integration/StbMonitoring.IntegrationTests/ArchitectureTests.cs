using StbMonitoring.Application.Interfaces;
using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.IntegrationTests;
public sealed class ArchitectureTests
{
    [Fact] public void Identity_store_implements_application_contract()=>Assert.Contains(typeof(IIdentityStore),typeof(IdentityStore).GetInterfaces());
    [Fact] public void Ai_prediction_service_implements_application_contract()=>Assert.Contains(typeof(IAiPredictionService),typeof(StbMonitoring.Infrastructure.AI.AiPredictionService).GetInterfaces());
}
