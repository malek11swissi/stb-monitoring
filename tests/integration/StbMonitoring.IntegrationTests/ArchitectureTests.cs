using StbMonitoring.Application.Interfaces;
using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.IntegrationTests;
public sealed class ArchitectureTests
{
    [Fact] public void Identity_store_implements_application_contract()=>Assert.Contains(typeof(IIdentityStore),typeof(IdentityStore).GetInterfaces());
}
