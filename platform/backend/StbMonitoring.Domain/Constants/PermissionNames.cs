namespace StbMonitoring.Domain.Constants;
public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string SystemsRead = "systems.read";
    public const string SystemsManage = "systems.manage";
    public const string ChecksRead = "checks.read";
    public const string ChecksExecute = "checks.execute";
    public static readonly string[] All = [UsersRead, UsersManage, RolesRead, RolesManage, AuditRead, SystemsRead, SystemsManage, ChecksRead, ChecksExecute];
}
