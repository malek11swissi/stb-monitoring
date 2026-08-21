namespace StbMonitoring.Domain.Constants;
public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string AuditRead = "audit.read";
    public const string SystemsRead = "systems.read";
    public const string SystemsManage = "systems.manage";
    public const string ChecksRead = "checks.read";
    public const string ChecksExecute = "checks.execute";
    public const string AlertsRead="alerts.read"; public const string AlertsAcknowledge="alerts.acknowledge"; public const string AlertRulesManage="alert_rules.manage";
    public const string IncidentsRead="incidents.read"; public const string IncidentsManage="incidents.manage"; public const string IncidentsAssign="incidents.assign"; public const string IncidentsWork="incidents.work"; public const string IncidentsResolve="incidents.resolve"; public const string IncidentsClose="incidents.close"; public const string IncidentsArchive="incidents.archive"; public const string IncidentsDelete="incidents.delete";
    public const string SlaManage="sla.manage"; public const string NotificationsRead="notifications.read"; public const string NotificationsManage="notifications.manage";
    public const string ReportingRead="reporting.read"; public const string ReportingExport="reporting.export";
    public const string MaintenanceRead="maintenance.read"; public const string MaintenanceManage="maintenance.manage";
    public static readonly string[] All = [UsersRead, UsersManage, AuditRead, SystemsRead, SystemsManage, ChecksRead, ChecksExecute,AlertsRead,AlertsAcknowledge,AlertRulesManage,IncidentsRead,IncidentsManage,IncidentsAssign,IncidentsWork,IncidentsResolve,IncidentsClose,IncidentsArchive,IncidentsDelete,SlaManage,NotificationsRead,NotificationsManage,ReportingRead,ReportingExport,MaintenanceRead,MaintenanceManage];

    public static IReadOnlyCollection<string> RolesFor(string permission) => permission switch
    {
        UsersRead or UsersManage or AuditRead or AlertRulesManage or SlaManage or NotificationsManage or IncidentsDelete => [RoleNames.Admin],
        IncidentsArchive => [RoleNames.Admin,RoleNames.Supervisor],
        SystemsManage or ChecksExecute or AlertsAcknowledge or IncidentsManage or IncidentsAssign or IncidentsClose or MaintenanceManage => [RoleNames.Admin, RoleNames.Supervisor],
        IncidentsWork or IncidentsResolve => [RoleNames.Admin, RoleNames.Supervisor, RoleNames.Technician],
        SystemsRead or ChecksRead or IncidentsRead or NotificationsRead or ReportingRead or ReportingExport or MaintenanceRead => RoleNames.All,
        AlertsRead => [RoleNames.Admin, RoleNames.Supervisor, RoleNames.ManagerIt],
        _ => [RoleNames.Admin]
    };
}
