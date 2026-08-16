using StbMonitoring.Domain.Entities;
namespace StbMonitoring.UnitTests;
public sealed class UserTests
{
    [Fact] public void New_user_is_active_and_normalizes_email(){var user=new User("malek","MALEK@EXAMPLE.COM","hash","Malek","Test");Assert.True(user.IsActive);Assert.Equal("malek@example.com",user.Email);}
    [Fact] public void Deactivate_and_activate_change_status(){var user=new User("malek","m@example.com","hash","Malek","Test");user.Deactivate();Assert.False(user.IsActive);user.Activate();Assert.True(user.IsActive);}
    [Fact] public void Record_login_sets_utc_timestamp(){var user=new User("malek","m@example.com","hash","Malek","Test");user.RecordLogin();Assert.NotNull(user.LastLoginAt);Assert.Equal(DateTimeKind.Utc,user.LastLoginAt!.Value.Kind);}
    [Fact] public void User_has_one_valid_role(){var user=new User("malek","m@example.com","hash","Malek","Test",StbMonitoring.Domain.Constants.RoleNames.Supervisor);Assert.Equal("SUPERVISOR",user.Role);Assert.Throws<ArgumentException>(()=>user.ChangeRole("UNKNOWN"));}
}
