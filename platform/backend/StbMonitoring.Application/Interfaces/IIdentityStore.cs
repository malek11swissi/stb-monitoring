using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Application.Interfaces;
public interface IIdentityStore
{
    Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken);
    Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetUsersAsync(CancellationToken cancellationToken);
    Task<Role?> FindRoleByNameAsync(string name, CancellationToken cancellationToken);
    Task<Role?> FindRoleByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetRolesAsync(CancellationToken cancellationToken);
    Task<Permission?> FindPermissionByNameAsync(string name, CancellationToken cancellationToken);
    Task<Permission?> FindPermissionByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken);
    Task<bool> UsernameOrEmailExistsAsync(string username, string email, Guid? excludedUserId, CancellationToken cancellationToken);
    void AddUser(User user);
    void AddRole(Role role);
    void RemoveRole(Role role);
    void AddPermission(Permission permission);
    void RemovePermission(Permission permission);
    void AddRolePermission(RolePermission rolePermission);
    void RemoveRolePermission(RolePermission rolePermission);
    void AddUserRole(UserRole userRole);
    void RemoveUserRole(UserRole userRole);
    void AddAudit(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
