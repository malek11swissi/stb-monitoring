using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Workers;

/// <summary>
/// Calcule l'escalade depuis la priorité et AssignedAt, sans table dédiée et
/// sans délai modifiable dans le profil. Les notifications internes servent
/// aussi de preuve d'envoi et empêchent une répétition du même niveau.
/// </summary>
public sealed class IncidentEscalationWorker(IServiceScopeFactory scopes,ILogger<IncidentEscalationWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(15),ct);
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();var channel=scope.ServiceProvider.GetRequiredService<INotificationChannel>();var now=DateTime.UtcNow;
                var active=await db.Incidents.Where(x=>x.AssignedAt!=null&&!x.IsArchived&&x.Status!=IncidentStatus.Resolved&&x.Status!=IncidentStatus.Closed&&x.Status!=IncidentStatus.Cancelled).ToArrayAsync(ct);
                foreach(var incident in active)
                {
                    var(supervisorMinutes,managerMinutes)=Delays(incident.Priority);var elapsed=(now-incident.AssignedAt!.Value).TotalMinutes;
                    if(elapsed>=supervisorMinutes)await NotifyOnce(db,channel,incident,RoleNames.Supervisor,"INCIDENT_ESCALATED_SUPERVISOR",ct);
                    if(elapsed>=managerMinutes)await NotifyOnce(db,channel,incident,RoleNames.ManagerIt,"INCIDENT_ESCALATED_MANAGER",ct);
                }
                await db.SaveChangesAsync(ct);
            }
            catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Erreur du worker d'escalade automatique.");}
            await Task.Delay(TimeSpan.FromSeconds(30),ct);
        }
    }

    private static async Task NotifyOnce(MonitoringDbContext db,INotificationChannel channel,Incident incident,string role,string type,CancellationToken ct)
    {
        if(await db.AuditLogs.AnyAsync(x=>x.EntityName=="Incident"&&x.EntityId==incident.Id&&x.Action==type,ct))return;
        var users=await db.Users.Where(x=>x.IsActive&&x.Role==role).ToArrayAsync(ct);var severity=incident.Priority==IncidentPriority.P1Critical?AlertSeverity.Critical:AlertSeverity.Major;
        foreach(var user in users)
        {
            var subject=$"[ESCALADE {role}] {incident.IncidentNumber} — {incident.Title}";
            if(user.InAppNotificationsEnabled)db.Notifications.Add(new Notification(user.Id,type,subject,$"Incident {incident.Priority} toujours non résolu.",severity,"Incident",incident.Id,$"/incidents/{incident.Id}"));
            if(user.EmailNotificationsEnabled)await channel.SendEmailAsync(user.Email,subject,$"<h2>{subject}</h2><p>Incident {incident.Priority} toujours non résolu.</p>",ct);
            if(user.SmsNotificationsEnabled&&!string.IsNullOrWhiteSpace(user.PhoneNumber))await channel.SendSmsAsync(user.PhoneNumber,subject,ct);
        }
        db.AuditLogs.Add(new AuditLog(null,type,"Incident",incident.Id,$"Rôle notifié : {role}; destinataires : {users.Length}",null));
    }

    private static(int Supervisor,int Manager)Delays(IncidentPriority priority)=>priority switch
    {
        IncidentPriority.P1Critical=>(5,15),IncidentPriority.P2High=>(15,30),IncidentPriority.P3Medium=>(30,60),_=>(60,120)
    };
}
