using Microsoft.AspNetCore.Authorization;
using StbMonitoring.Api.Controllers;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.IntegrationTests;
public sealed class AuthorizationTests
{
 [Fact]public void Users_list_requires_read_permission(){var method=typeof(UsersController).GetMethod("All")!;Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>(),x=>x.Policy==PermissionNames.UsersRead);}
 [Fact]public void Roles_mutation_requires_manage_permission(){var method=typeof(RolesController).GetMethod("Create")!;Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>(),x=>x.Policy==PermissionNames.RolesManage);}
}
