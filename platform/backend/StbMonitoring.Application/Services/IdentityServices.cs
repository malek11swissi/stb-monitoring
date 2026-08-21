using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Application.Services;

/// <summary>
/// Porte les cas d'utilisation d'authentification. Ce service vérifie l'état du
/// compte, contrôle le mot de passe, émet le JWT et trace chaque tentative dans
/// l'audit. Il ne décide pas des droits fonctionnels : ceux-ci sont appliqués
/// ensuite par les politiques d'autorisation à partir du rôle contenu dans le JWT.
/// </summary>
public sealed class AuthService(IIdentityStore store, IPasswordService passwords, ITokenService tokens) : IAuthService
{
    /// <summary>Renouvelle le JWT seulement si le compte existe toujours et reste actif.</summary>
    public async Task<LoginResponse> RefreshAsync(Guid userId, CancellationToken ct)
    {
        var user = await store.FindUserByIdAsync(userId, ct) ?? throw new KeyNotFoundException("Utilisateur introuvable.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Compte désactivé.");
        var token = tokens.Generate(new(user.Id, user.Username, user.Email, user.Role));
        return new(token.Token, token.ExpiresAt, Map(user));
    }
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        var user = await store.FindUserByLoginAsync(request.UsernameOrEmail, ct);
        // On retourne volontairement le même résultat pour un compte inconnu,
        // désactivé ou un mauvais mot de passe afin de ne pas révéler les comptes existants.
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, request.Password))
        { store.AddAudit(new AuditLog(user?.Id, "LOGIN_FAILED", "User", user?.Id, "Identifiants invalides", ip)); await store.SaveChangesAsync(ct); return null; }
        user.RecordLogin();
        var token = tokens.Generate(new(user.Id, user.Username, user.Email, user.Role));
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
    internal static UserResponse Map(User u) => new(u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt);
}

/// <summary>
/// Gère le cycle de vie administratif des comptes. Les garde-fous empêchent
/// notamment la suppression fonctionnelle du dernier administrateur actif.
/// </summary>
public sealed class UserService(IIdentityStore store, IPasswordService passwords) : IUserService
{
    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(CancellationToken ct) => (await store.GetUsersAsync(ct)).Select(AuthService.Map).ToArray();
    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken ct) { var user = await store.FindUserByIdAsync(id, ct); return user is null ? null : AuthService.Map(user); }
    public async Task<UserResponse> CreateAsync(CreateUserRequest r, Guid actor, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Username) || string.IsNullOrWhiteSpace(r.Email) || r.Password.Length < 8) throw new ArgumentException("Nom, e-mail et mot de passe (8 caractères minimum) sont obligatoires.");
        if (await store.UsernameOrEmailExistsAsync(r.Username, r.Email, null, ct)) throw new InvalidOperationException("Nom d’utilisateur ou e-mail déjà utilisé.");
        var user = new User(r.Username, r.Email, passwords.Hash(r.Password), r.FirstName, r.LastName, r.Role); store.AddUser(user);
        store.AddAudit(new AuditLog(actor, "USER_CREATED", "User", user.Id, user.Username, ip)); await store.SaveChangesAsync(ct);
        return (await GetByIdAsync(user.Id, ct))!;
    }
    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest r, Guid actor, string? ip, CancellationToken ct)
    { var user = await Required(id, ct); if (await store.UsernameOrEmailExistsAsync(user.Username, r.Email, id, ct)) throw new InvalidOperationException("E-mail déjà utilisé.");
      // La plateforme doit toujours conserver au moins un administrateur actif.
      if (!string.IsNullOrWhiteSpace(r.Role) && user.Role == StbMonitoring.Domain.Constants.RoleNames.Admin && StbMonitoring.Domain.Constants.RoleNames.Normalize(r.Role) != StbMonitoring.Domain.Constants.RoleNames.Admin && await store.CountActiveAdminsAsync(ct) <= 1) throw new InvalidOperationException("Le dernier administrateur actif ne peut pas changer de rôle."); user.UpdateProfile(r.FirstName, r.LastName, r.Email); if (!string.IsNullOrWhiteSpace(r.Role)) user.ChangeRole(r.Role); Audit(actor,"USER_UPDATED",user,ip,$"Rôle: {user.Role}"); await store.SaveChangesAsync(ct); return AuthService.Map(user); }
    public async Task SetActiveAsync(Guid id, bool active, Guid actor, string? ip, CancellationToken ct)
    { var user=await Required(id,ct); if(!active&&id==actor)throw new InvalidOperationException("Vous ne pouvez pas désactiver votre propre compte."); if(!active&&user.Role==StbMonitoring.Domain.Constants.RoleNames.Admin&&await store.CountActiveAdminsAsync(ct)<=1)throw new InvalidOperationException("Le dernier administrateur actif ne peut pas être désactivé."); if(active) user.Activate(); else user.Deactivate(); Audit(actor,active?"USER_ACTIVATED":"USER_DEACTIVATED",user,ip); await store.SaveChangesAsync(ct); }
    private async Task<User> Required(Guid id,CancellationToken ct)=>await store.FindUserByIdAsync(id,ct)??throw new KeyNotFoundException("Utilisateur introuvable.");
    private void Audit(Guid actor,string action,User user,string? ip,string? detail=null)=>store.AddAudit(new AuditLog(actor,action,"User",user.Id,detail,ip));
}
