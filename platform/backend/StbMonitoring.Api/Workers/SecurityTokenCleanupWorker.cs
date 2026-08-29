using Microsoft.EntityFrameworkCore;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Workers;

/// <summary>
/// Efface les secrets de récupération et de double authentification expirés.
/// Un jeton expiré était déjà inutilisable ; ce nettoyage réduit en plus la
/// quantité de données de sécurité conservée dans PostgreSQL.
/// </summary>
public sealed class SecurityTokenCleanupWorker(IServiceScopeFactory scopes,ILogger<SecurityTokenCleanupWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();var now=DateTime.UtcNow;
                var users=await db.Users.Where(x=>(x.PasswordResetExpiresAt!=null&&x.PasswordResetExpiresAt<=now)||(x.TwoFactorExpiresAt!=null&&x.TwoFactorExpiresAt<=now)).ToArrayAsync(ct);
                foreach(var user in users)user.ClearExpiredSecurityTokens(now);
                if(users.Length>0){await db.SaveChangesAsync(ct);logger.LogInformation("{Count} jeton(s) de sécurité expiré(s) nettoyé(s).",users.Length);}
            }
            catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Échec du nettoyage des jetons de sécurité.");}
            await Task.Delay(TimeSpan.FromMinutes(15),ct);
        }
    }
}
