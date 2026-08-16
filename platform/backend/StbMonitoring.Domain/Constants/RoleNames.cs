namespace StbMonitoring.Domain.Constants;
public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string ManagerIt = "MANAGER_IT";
    public const string Supervisor = "SUPERVISOR";
    public const string Technician = "TECHNICIAN";
    public static readonly string[] All = [Admin, ManagerIt, Supervisor, Technician];

    public static string Normalize(string role)
    {
        var normalized = role.Trim().ToUpperInvariant();
        return All.Contains(normalized) ? normalized : throw new ArgumentException($"Rôle invalide : {role}.");
    }
}
