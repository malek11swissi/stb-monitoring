using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Infrastructure.Persistence;
public sealed class IdentityStore(MonitoringDbContext db) : IIdentityStore
{
    private IQueryable<User> Users => db.Users.Include(x=>x.UserRoles).ThenInclude(x=>x.Role).ThenInclude(x=>x.RolePermissions).ThenInclude(x=>x.Permission);
    public Task<User?> FindUserByLoginAsync(string login,CancellationToken ct){login=login.Trim().ToLowerInvariant();return Users.SingleOrDefaultAsync(x=>x.Username.ToLower()==login||x.Email==login,ct);}
    public Task<User?> FindUserByIdAsync(Guid id,CancellationToken ct)=>Users.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<IReadOnlyCollection<User>> GetUsersAsync(CancellationToken ct)=>await Users.OrderBy(x=>x.Username).ToArrayAsync(ct);
    public Task<Role?> FindRoleByNameAsync(string name,CancellationToken ct)=>db.Roles.Include(x=>x.RolePermissions).ThenInclude(x=>x.Permission).SingleOrDefaultAsync(x=>x.Name==name.Trim().ToUpper(),ct);
    public async Task<IReadOnlyCollection<Role>> GetRolesAsync(CancellationToken ct)=>await db.Roles.Include(x=>x.RolePermissions).ThenInclude(x=>x.Permission).OrderBy(x=>x.Name).ToArrayAsync(ct);
    public Task<bool> UsernameOrEmailExistsAsync(string username,string email,Guid? excluded,CancellationToken ct)=>db.Users.AnyAsync(x=>(!excluded.HasValue||x.Id!=excluded)&&(x.Username.ToLower()==username.Trim().ToLower()||x.Email==email.Trim().ToLower()),ct);
    public void AddUser(User x)=>db.Users.Add(x); public void AddUserRole(UserRole x)=>db.UserRoles.Add(x); public void RemoveUserRole(UserRole x)=>db.UserRoles.Remove(x); public void AddAudit(AuditLog x)=>db.AuditLogs.Add(x);
    public async Task SaveChangesAsync(CancellationToken ct)=>await db.SaveChangesAsync(ct);
}
