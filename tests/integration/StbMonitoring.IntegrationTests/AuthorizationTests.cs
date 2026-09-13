using Microsoft.AspNetCore.Authorization;
using StbMonitoring.Api.Controllers;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.IntegrationTests;
public sealed class AuthorizationTests
{
 [Fact]public void Users_list_requires_read_permission(){var method=typeof(UsersController).GetMethod("All")!;Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>(),x=>x.Policy==PermissionNames.UsersRead);}
 [Fact]public void Users_mutation_requires_manage_permission(){var method=typeof(UsersController).GetMethod("Create")!;Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>(),x=>x.Policy==PermissionNames.UsersManage);}
 [Fact]public void Ai_system_risk_is_restricted_to_operational_decision_roles(){var method=typeof(AiPredictionsController).GetMethod(nameof(AiPredictionsController.SystemRisk))!;var attribute=method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single();Assert.Contains(RoleNames.Supervisor,attribute.Roles);Assert.Contains(RoleNames.ManagerIt,attribute.Roles);Assert.DoesNotContain(RoleNames.Technician,attribute.Roles);}
}
