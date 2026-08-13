namespace StbMonitoring.Domain.Constants;
public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public static readonly string[] All = [UsersRead, UsersManage, RolesManage];
}
