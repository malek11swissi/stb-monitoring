using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Infrastructure.Persistence;

public sealed class DatabaseSeeder(MonitoringDbContext db, IPasswordService passwords)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        var existingRoles = await db.Roles.Select(x => x.Name).ToArrayAsync(ct);
        foreach (var name in RoleNames.All.Except(existingRoles, StringComparer.OrdinalIgnoreCase))
            db.Roles.Add(new Role(name, $"Rôle {name}"));

        var existingPermissions = await db.Permissions.Select(x => x.Name).ToArrayAsync(ct);
        foreach (var name in PermissionNames.All.Except(existingPermissions, StringComparer.OrdinalIgnoreCase))
            db.Permissions.Add(new Permission(name, $"Permission {name}"));

        await db.SaveChangesAsync(ct);

        var admin = await db.Roles.SingleAsync(x => x.Name == RoleNames.Admin, ct);
        var permissions = await db.Permissions.ToArrayAsync(ct);
        var assignedIds = await db.RolePermissions.Where(x => x.RoleId == admin.Id).Select(x => x.PermissionId).ToArrayAsync(ct);
        foreach (var permission in permissions.Where(x => !assignedIds.Contains(x.Id)))
            db.RolePermissions.Add(new RolePermission(admin.Id, permission.Id));

        if (!await db.Users.AnyAsync(ct))
        {
            var user = new User("admin", "admin@stb.local", passwords.Hash("ChangeMe123!"), "Admin", "STB");
            db.Users.Add(user);
            db.UserRoles.Add(new UserRole(user.Id, admin.Id));
        }

        await db.SaveChangesAsync(ct);
    }
}
