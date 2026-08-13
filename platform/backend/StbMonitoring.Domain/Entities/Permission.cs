namespace StbMonitoring.Domain.Entities;
public sealed class Permission
{
    private Permission() { }
    public Permission(string name, string description) { Id = Guid.NewGuid(); Name = name.Trim().ToLowerInvariant(); Description = description.Trim(); }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
    public void Update(string description) => Description = description.Trim();
}
