namespace StbMonitoring.Application.Contracts;

public sealed record LoginRequest(string UsernameOrEmail, string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);
public sealed record CreateUserRequest(string Username, string Email, string Password, string FirstName, string LastName, IReadOnlyCollection<string>? Roles);
public sealed record UpdateUserRequest(string Email, string FirstName, string LastName);
public sealed record UpdateProfileRequest(string Email, string FirstName, string LastName);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AssignRoleRequest(string RoleName);
public sealed record UserResponse(Guid Id, string Username, string Email, string FirstName, string LastName, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, IReadOnlyCollection<string> Roles);
public sealed record RoleResponse(Guid Id, string Name, string Description, IReadOnlyCollection<string> Permissions);
public sealed record AuthenticatedUser(Guid Id, string Username, string Email, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record TokenResult(string Token, DateTime ExpiresAt);
