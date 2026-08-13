namespace StbMonitoring.Domain.Entities;
public sealed class UserRole
{
    private UserRole() { }
    public UserRole(Guid userId, Guid roleId, Guid? assignedBy = null) { UserId = userId; RoleId = roleId; AssignedBy = assignedBy; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;
    public Guid? AssignedBy { get; private set; }
    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;
}
