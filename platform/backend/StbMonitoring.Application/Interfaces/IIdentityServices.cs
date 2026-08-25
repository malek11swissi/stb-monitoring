using StbMonitoring.Application.Contracts;
namespace StbMonitoring.Application.Interfaces;
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<LoginResponse> RefreshAsync(Guid userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken);
}
public interface IUserService
{
    Task<IReadOnlyCollection<UserResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, Guid actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, Guid actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<UserResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid id, bool active, Guid actorId, string? ipAddress, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAvatarAsync(Guid id, string? avatarPath, Guid actorId, string? ipAddress, CancellationToken cancellationToken);
}
