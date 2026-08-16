using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Application.Interfaces;
public interface IIdentityStore
{
    Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken);
    Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken);
    Task<bool> UsernameOrEmailExistsAsync(string username, string email, Guid? excludedUserId, CancellationToken cancellationToken);
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);
    void AddUser(User user);
    void AddAudit(AuditLog auditLog);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
