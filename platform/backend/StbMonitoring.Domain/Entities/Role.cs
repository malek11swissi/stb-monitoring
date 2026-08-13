namespace StbMonitoring.Domain.Entities;
public sealed class Role
{
    private Role() { }
    public Role(string name, string description) { Id = Guid.NewGuid(); Name = name.Trim().ToUpperInvariant(); Description = description.Trim(); }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
    public void Update(string name, string description) { Name = name.Trim().ToUpperInvariant(); Description = description.Trim(); }
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
