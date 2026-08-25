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
    public bool InAppNotificationsEnabled { get; private set; } = true;
    public bool EmailNotificationsEnabled { get; private set; } = true;
    public bool SmsNotificationsEnabled { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool CriticalNotificationsOnly { get; private set; } = true;
    public string? AvatarPath { get; private set; }
    public string Skills { get; private set; } = string.Empty;
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetExpiresAt { get; private set; }
    public void UpdateProfile(string firstName, string lastName, string email) { FirstName = firstName.Trim(); LastName = lastName.Trim(); Email = email.Trim().ToLowerInvariant(); }
    public void UpdateSkills(IEnumerable<string> skills) => Skills = string.Join('|', skills.Select(x => x.Trim()).Where(x => x.Length is > 0 and <= 60).Distinct(StringComparer.OrdinalIgnoreCase).Take(12));
    public void UpdateAvatar(string? avatarPath) => AvatarPath = string.IsNullOrWhiteSpace(avatarPath) ? null : avatarPath;
    public void ChangePassword(string passwordHash) => PasswordHash = passwordHash;
    public void BeginPasswordReset(string tokenHash, DateTime expiresAt) { PasswordResetTokenHash = tokenHash; PasswordResetExpiresAt = expiresAt; }
    public bool CanResetPassword(string tokenHash, DateTime now) => PasswordResetTokenHash == tokenHash && PasswordResetExpiresAt > now;
    public void CompletePasswordReset(string passwordHash) { PasswordHash = passwordHash; PasswordResetTokenHash = null; PasswordResetExpiresAt = null; }
    public void ChangeRole(string role) => Role = Constants.RoleNames.Normalize(role);
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
    public void UpdateNotificationPreferences(bool inApp,bool email,bool sms,string? phone,bool criticalOnly){InAppNotificationsEnabled=inApp;EmailNotificationsEnabled=email;SmsNotificationsEnabled=sms;PhoneNumber=phone?.Trim();CriticalNotificationsOnly=criticalOnly;}
}
