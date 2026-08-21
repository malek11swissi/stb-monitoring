namespace StbMonitoring.Domain.Entities;
/// <summary>Compte authentifiable et rôle fixe utilisé par les autorisations.</summary>

public sealed class User
{
    private User() { }
    public User(string username, string email, string passwordHash, string firstName, string lastName, string role = Constants.RoleNames.Technician)
    {
        Id = Guid.NewGuid(); Username = username.Trim(); Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash; FirstName = firstName.Trim(); LastName = lastName.Trim(); Role = Constants.RoleNames.Normalize(role);
    }
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string Role { get; private set; } = Constants.RoleNames.Technician;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public void UpdateProfile(string firstName, string lastName, string email) { FirstName = firstName.Trim(); LastName = lastName.Trim(); Email = email.Trim().ToLowerInvariant(); }
    public void ChangePassword(string passwordHash) => PasswordHash = passwordHash;
    public void ChangeRole(string role) => Role = Constants.RoleNames.Normalize(role);
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
}
