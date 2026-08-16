using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.UnitTests;
public sealed class SprintOneTests
{
    [Fact] public void Password_is_never_exposed_by_user(){Assert.Null(typeof(User).GetProperty("Password"));Assert.NotNull(typeof(User).GetProperty("PasswordHash"));}
    [Fact] public void Failed_audit_is_recorded_as_unsuccessful(){var log=new AuditLog(null,"LOGIN_FAILED","User",null,null,"127.0.0.1",false);Assert.False(log.Success);}
    [Fact] public void Permission_names_are_unique()=>Assert.Equal(PermissionNames.All.Length,PermissionNames.All.Distinct().Count());
    [Fact] public void Fixed_roles_are_unique()=>Assert.Equal(RoleNames.All.Length,RoleNames.All.Distinct().Count());
    [Fact] public void Administrator_has_access_to_every_policy()=>Assert.All(PermissionNames.All,p=>Assert.Contains(RoleNames.Admin,PermissionNames.RolesFor(p)));
}
