using System.ComponentModel.DataAnnotations;
namespace StbMonitoring.Application.Contracts;

public sealed record LoginRequest([Required] string UsernameOrEmail, [Required] string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);
public sealed record RefreshRequest(string Token);
public sealed record CreateUserRequest([Required,MinLength(3)] string Username, [Required,EmailAddress] string Email, [Required,MinLength(8)] string Password, [Required] string FirstName, [Required] string LastName, [Required] string Role);
public sealed record UpdateUserRequest([Required,EmailAddress] string Email, [Required] string FirstName, [Required] string LastName, string? Role = null);
public sealed record UpdateProfileRequest([Required,EmailAddress] string Email, [Required] string FirstName, [Required] string LastName);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required,MinLength(8)] string NewPassword);
public sealed record UserResponse(Guid Id, string Username, string Email, string FirstName, string LastName, string Role, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);
public sealed record AuditLogResponse(Guid Id, Guid? UserId, string Action, string EntityName, Guid? EntityId, string? Detail, string? IpAddress, DateTime CreatedAt, bool Success);
public sealed record AuthenticatedUser(Guid Id, string Username, string Email, string Role);
public sealed record TokenResult(string Token, DateTime ExpiresAt);
