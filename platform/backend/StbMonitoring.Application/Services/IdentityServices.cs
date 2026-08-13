using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Application.Services;

public sealed class AuthService(IIdentityStore store, IPasswordService passwords, ITokenService tokens) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        var user = await store.FindUserByLoginAsync(request.UsernameOrEmail, ct);
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, request.Password))
        { store.AddAudit(new AuditLog(user?.Id, "LOGIN_FAILED", "User", user?.Id, "Identifiants invalides", ip)); await store.SaveChangesAsync(ct); return null; }
        user.RecordLogin();
        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().ToArray();
        var permissions = user.UserRoles.SelectMany(x => x.Role.RolePermissions).Select(x => x.Permission.Name).Distinct().ToArray();
        var token = tokens.Generate(new(user.Id, user.Username, user.Email, roles, permissions));
        store.AddAudit(new AuditLog(user.Id, "LOGIN_SUCCESS", "User", user.Id, null, ip)); await store.SaveChangesAsync(ct);
        return new(token.Token, token.ExpiresAt, Map(user));
    }
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ip, CancellationToken ct)
    {
        var user = await store.FindUserByIdAsync(userId, ct) ?? throw new KeyNotFoundException("Utilisateur introuvable.");
        if (!passwords.Verify(user.PasswordHash, request.CurrentPassword)) throw new InvalidOperationException("Mot de passe actuel incorrect.");
        if (request.NewPassword.Length < 8) throw new ArgumentException("Le nouveau mot de passe doit contenir au moins 8 caractères.");
        user.ChangePassword(passwords.Hash(request.NewPassword)); store.AddAudit(new AuditLog(userId, "PASSWORD_CHANGED", "User", userId, null, ip)); await store.SaveChangesAsync(ct);
    }
    internal static UserResponse Map(User u) => new(u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.IsActive, u.CreatedAt, u.LastLoginAt, u.UserRoles.Select(x => x.Role.Name).Distinct().ToArray());
}

public sealed class UserService(IIdentityStore store, IPasswordService passwords) : IUserService
{
    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(CancellationToken ct) => (await store.GetUsersAsync(ct)).Select(AuthService.Map).ToArray();
    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken ct) { var user = await store.FindUserByIdAsync(id, ct); return user is null ? null : AuthService.Map(user); }
    public async Task<UserResponse> CreateAsync(CreateUserRequest r, Guid actor, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Username) || string.IsNullOrWhiteSpace(r.Email) || r.Password.Length < 8) throw new ArgumentException("Nom, e-mail et mot de passe (8 caractères minimum) sont obligatoires.");
        if (await store.UsernameOrEmailExistsAsync(r.Username, r.Email, null, ct)) throw new InvalidOperationException("Nom d’utilisateur ou e-mail déjà utilisé.");
        var user = new User(r.Username, r.Email, passwords.Hash(r.Password), r.FirstName, r.LastName); store.AddUser(user);
        foreach (var roleName in r.Roles ?? ["USER"]) { var role = await store.FindRoleByNameAsync(roleName, ct) ?? throw new KeyNotFoundException($"Rôle {roleName} introuvable."); store.AddUserRole(new(user.Id, role.Id, actor)); }
        store.AddAudit(new AuditLog(actor, "USER_CREATED", "User", user.Id, user.Username, ip)); await store.SaveChangesAsync(ct);
        return (await GetByIdAsync(user.Id, ct))!;
    }
    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest r, Guid actor, string? ip, CancellationToken ct)
    { var user = await Required(id, ct); if (await store.UsernameOrEmailExistsAsync(user.Username, r.Email, id, ct)) throw new InvalidOperationException("E-mail déjà utilisé."); user.UpdateProfile(r.FirstName, r.LastName, r.Email); Audit(actor,"USER_UPDATED",user,ip); await store.SaveChangesAsync(ct); return AuthService.Map(user); }
    public async Task SetActiveAsync(Guid id, bool active, Guid actor, string? ip, CancellationToken ct)
    { var user=await Required(id,ct); if(active) user.Activate(); else user.Deactivate(); Audit(actor,active?"USER_ACTIVATED":"USER_DEACTIVATED",user,ip); await store.SaveChangesAsync(ct); }
    public async Task AssignRoleAsync(Guid id, AssignRoleRequest r, Guid actor, string? ip, CancellationToken ct)
    { var user=await Required(id,ct); var role=await store.FindRoleByNameAsync(r.RoleName,ct)??throw new KeyNotFoundException("Rôle introuvable."); if(user.UserRoles.All(x=>x.RoleId!=role.Id)) store.AddUserRole(new(id,role.Id,actor)); Audit(actor,"ROLE_ASSIGNED",user,ip,r.RoleName); await store.SaveChangesAsync(ct); }
    public async Task RemoveRoleAsync(Guid id, string roleName, Guid actor, string? ip, CancellationToken ct)
    { var user=await Required(id,ct); var link=user.UserRoles.FirstOrDefault(x=>x.Role.Name.Equals(roleName,StringComparison.OrdinalIgnoreCase))??throw new KeyNotFoundException("Rôle non attribué."); store.RemoveUserRole(link); Audit(actor,"ROLE_REMOVED",user,ip,roleName); await store.SaveChangesAsync(ct); }
    private async Task<User> Required(Guid id,CancellationToken ct)=>await store.FindUserByIdAsync(id,ct)??throw new KeyNotFoundException("Utilisateur introuvable.");
    private void Audit(Guid actor,string action,User user,string? ip,string? detail=null)=>store.AddAudit(new AuditLog(actor,action,"User",user.Id,detail,ip));
}
