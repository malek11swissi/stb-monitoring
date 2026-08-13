using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Infrastructure.Persistence;
public sealed class DatabaseSeeder(MonitoringDbContext db,IPasswordService passwords)
{
    public async Task SeedAsync(CancellationToken ct=default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if(!await db.Roles.AnyAsync(ct)){foreach(var n in RoleNames.All)db.Roles.Add(new Role(n,$"Rôle {n}"));await db.SaveChangesAsync(ct);}
        if(!await db.Permissions.AnyAsync(ct)){foreach(var n in PermissionNames.All)db.Permissions.Add(new Permission(n,$"Permission {n}"));await db.SaveChangesAsync(ct);}
        var admin=await db.Roles.SingleAsync(x=>x.Name==RoleNames.Admin,ct);var perms=await db.Permissions.ToArrayAsync(ct);
        if(!await db.RolePermissions.AnyAsync(ct)){foreach(var p in perms)db.RolePermissions.Add(new RolePermission(admin.Id,p.Id));}
        if(!await db.Users.AnyAsync(ct)){var user=new User("admin","admin@stb.local",passwords.Hash("ChangeMe123!"),"Admin","STB");db.Users.Add(user);db.UserRoles.Add(new UserRole(user.Id,admin.Id));}
        await db.SaveChangesAsync(ct);
    }
}
