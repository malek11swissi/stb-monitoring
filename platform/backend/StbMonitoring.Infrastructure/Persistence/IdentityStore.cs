using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Infrastructure.Persistence;
/// <summary>Implémentation PostgreSQL des recherches utilisateurs et de l'audit.</summary>
public sealed class IdentityStore(MonitoringDbContext db) : IIdentityStore
{
    private IQueryable<User> Users => db.Users;
    public Task<User?> FindUserByLoginAsync(string login,CancellationToken ct){login=login.Trim().ToLowerInvariant();return Users.SingleOrDefaultAsync(x=>x.Username.ToLower()==login||x.Email==login,ct);}
    public Task<User?> FindUserByIdAsync(Guid id,CancellationToken ct)=>Users.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<IReadOnlyCollection<User>> GetUsersAsync(CancellationToken ct)=>await Users.OrderBy(x=>x.Username).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken ct)=>await db.AuditLogs.OrderByDescending(x=>x.CreatedAt).Take(500).ToArrayAsync(ct);
    public Task<bool> UsernameOrEmailExistsAsync(string username,string email,Guid? excluded,CancellationToken ct)=>db.Users.AnyAsync(x=>(!excluded.HasValue||x.Id!=excluded)&&(x.Username.ToLower()==username.Trim().ToLower()||x.Email==email.Trim().ToLower()),ct);
    public Task<int> CountActiveAdminsAsync(CancellationToken ct)=>db.Users.CountAsync(x=>x.IsActive&&x.Role==StbMonitoring.Domain.Constants.RoleNames.Admin,ct);
    public Task<int> CountResolvedIncidentsAsync(Guid userId,CancellationToken ct)=>db.Incidents.CountAsync(x=>x.AssignedToUserId==userId&&x.ResolvedAt!=null,ct);
    public async Task<IReadOnlyCollection<StbMonitoring.Application.Contracts.TechnicianResolvedIncidentStat>> GetTechnicianResolvedIncidentStatsAsync(Guid userId,CancellationToken ct)
    {
        var incidents=await db.Incidents.AsNoTracking().Where(x=>x.AssignedToUserId==userId&&x.ResolvedAt!=null).ToArrayAsync(ct);
        return incidents.Select(x=>new StbMonitoring.Application.Contracts.TechnicianResolvedIncidentStat(x.Category.ToString(),x.Priority.ToString(),x.SlaStatus.ToString(),x.ReopenCount,x.CreatedAt,x.ResolvedAt,x.SystemId)).ToArray();
    }
    public void AddUser(User x)=>db.Users.Add(x); public void AddAudit(AuditLog x)=>db.AuditLogs.Add(x);
    public async Task SaveChangesAsync(CancellationToken ct)=>await db.SaveChangesAsync(ct);
}
