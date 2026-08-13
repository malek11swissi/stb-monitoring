namespace StbMonitoring.Domain.Constants;
public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string ManagerIt = "MANAGER_IT";
    public const string Supervisor = "SUPERVISOR";
    public const string Technician = "TECHNICIAN";
    public const string User = "USER";
    public static readonly string[] All = [Admin, ManagerIt, Supervisor, Technician, User];
}
