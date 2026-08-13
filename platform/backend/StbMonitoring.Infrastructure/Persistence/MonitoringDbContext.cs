using Microsoft.EntityFrameworkCore;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Infrastructure.Persistence;
public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>(); public DbSet<Role> Roles => Set<Role>(); public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>(); public DbSet<RolePermission> RolePermissions => Set<RolePermission>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e=>{e.ToTable("users");e.HasKey(x=>x.Id);e.Property(x=>x.Username).HasMaxLength(80).IsRequired();e.HasIndex(x=>x.Username).IsUnique();e.Property(x=>x.Email).HasMaxLength(180).IsRequired();e.HasIndex(x=>x.Email).IsUnique();e.Property(x=>x.PasswordHash).IsRequired();});
        b.Entity<Role>(e=>{e.ToTable("roles");e.HasKey(x=>x.Id);e.Property(x=>x.Name).HasMaxLength(60).IsRequired();e.HasIndex(x=>x.Name).IsUnique();});
        b.Entity<Permission>(e=>{e.ToTable("permissions");e.HasKey(x=>x.Id);e.Property(x=>x.Name).HasMaxLength(100).IsRequired();e.HasIndex(x=>x.Name).IsUnique();});
        b.Entity<UserRole>(e=>{e.ToTable("user_roles");e.HasKey(x=>new{x.UserId,x.RoleId});e.HasOne(x=>x.User).WithMany(x=>x.UserRoles).HasForeignKey(x=>x.UserId);e.HasOne(x=>x.Role).WithMany(x=>x.UserRoles).HasForeignKey(x=>x.RoleId);});
        b.Entity<RolePermission>(e=>{e.ToTable("role_permissions");e.HasKey(x=>new{x.RoleId,x.PermissionId});e.HasOne(x=>x.Role).WithMany(x=>x.RolePermissions).HasForeignKey(x=>x.RoleId);e.HasOne(x=>x.Permission).WithMany(x=>x.RolePermissions).HasForeignKey(x=>x.PermissionId);});
        b.Entity<AuditLog>(e=>{e.ToTable("audit_logs");e.HasKey(x=>x.Id);e.Property(x=>x.Action).HasMaxLength(100).IsRequired();e.Property(x=>x.EntityName).HasMaxLength(100).IsRequired();});
    }
}
