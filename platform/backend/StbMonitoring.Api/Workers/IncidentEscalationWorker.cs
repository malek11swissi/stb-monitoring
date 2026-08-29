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
public sealed class IncidentEscalationWorker(IServiceScopeFactory scopes,ILogger<IncidentEscalationWorker> logger,IConfiguration configuration):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(15),ct);
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();var channel=scope.ServiceProvider.GetRequiredService<INotificationChannel>();var now=DateTime.UtcNow;
                var unresolved=await db.Incidents.Where(x=>!x.IsArchived&&x.Status!=IncidentStatus.Resolved&&x.Status!=IncidentStatus.Closed&&x.Status!=IncidentStatus.Cancelled).ToArrayAsync(ct);
                // Avertit le Manager IT lorsqu'aucun technicien n'a été affecté à
                // 75 % du délai de réponse : le SLA est encore récupérable.
                foreach(var incident in unresolved.Where(x=>x.AssignedToUserId==null&&x.ResponseDueAt.HasValue&&now>=x.CreatedAt.AddTicks((x.ResponseDueAt.Value-x.CreatedAt).Ticks*75/100)))
                    await NotifyManagerUnassignedOnce(db,channel,incident,now,configuration["FrontendUrl"]??"http://localhost:4200",ct);
                var active=unresolved.Where(x=>x.AssignedAt!=null).ToArray();
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

    private static async Task NotifyManagerUnassignedOnce(MonitoringDbContext db,INotificationChannel channel,Incident incident,DateTime now,string frontendUrl,CancellationToken ct)
    {
        const string type="INCIDENT_UNASSIGNED_SLA_RISK";
        if(await db.AuditLogs.AnyAsync(x=>x.EntityName=="Incident"&&x.EntityId==incident.Id&&x.Action==type,ct))return;
        var system=incident.SystemId.HasValue?await db.Systems.AsNoTracking().Where(x=>x.Id==incident.SystemId).Select(x=>x.Name).FirstOrDefaultAsync(ct):null;
        var endpoint=incident.EndpointId.HasValue?await db.MonitoringEndpoints.AsNoTracking().Where(x=>x.Id==incident.EndpointId).Select(x=>x.Name).FirstOrDefaultAsync(ct):null;
        var remaining=incident.ResponseDueAt!.Value-now;var remainingText=remaining<=TimeSpan.Zero?"délai de réponse dépassé":remaining.TotalHours>=1?$"{Math.Floor(remaining.TotalHours)} h {remaining.Minutes} min":$"{Math.Max(0,remaining.Minutes)} min";
        var managers=await db.Users.Where(x=>x.IsActive&&x.Role==RoleNames.ManagerIt).ToArrayAsync(ct);
        foreach(var manager in managers)
        {
            var subject=$"[SLA À RISQUE] {incident.IncidentNumber} — aucun technicien affecté";
            var message=$"Le superviseur n’a pas encore affecté de technicien. Temps restant avant l’échéance de prise en charge : {remainingText}.";
            if(manager.InAppNotificationsEnabled)db.Notifications.Add(new Notification(manager.Id,type,subject,message,AlertSeverity.Critical,"Incident",incident.Id,$"/incidents/{incident.Id}"));
            if(manager.EmailNotificationsEnabled)
            {
                static string E(string? value)=>System.Net.WebUtility.HtmlEncode(value??"Non renseigné");
                var url=$"{frontendUrl.TrimEnd('/')}/incidents/{incident.Id}";
                var html=$$"""<div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;color:#18252e;border:1px solid #f1c7c4;border-radius:14px;overflow:hidden"><div style="background:#8f1c13;color:white;padding:24px"><small>STB MONITORING · ESCALADE MANAGER IT</small><h1 style="font-size:23px">Incident sans technicien affecté</h1></div><div style="padding:24px"><p>Une intervention de pilotage est nécessaire : le superviseur n’a pas encore affecté cet incident à un technicien et le délai SLA approche.</p><h2>{{E(incident.IncidentNumber)}} — {{E(incident.Title)}}</h2><table style="width:100%;border-collapse:collapse"><tr><td style="padding:8px;color:#61727e">SI</td><td>{{E(system)}}</td></tr><tr><td style="padding:8px;color:#61727e">Endpoint</td><td>{{E(endpoint)}}</td></tr><tr><td style="padding:8px;color:#61727e">Priorité</td><td><strong>{{E(incident.Priority.ToString())}}</strong></td></tr><tr><td style="padding:8px;color:#61727e">Créé le</td><td>{{incident.CreatedAt:dd/MM/yyyy HH:mm}} UTC</td></tr><tr><td style="padding:8px;color:#61727e">Échéance de prise en charge</td><td>{{incident.ResponseDueAt:dd/MM/yyyy HH:mm}} UTC</td></tr><tr><td style="padding:8px;color:#61727e">Temps restant</td><td style="color:#b42318;font-weight:bold">{{remainingText}}</td></tr></table><p style="margin-top:22px">Veuillez vérifier l’affectation avec le superviseur afin d’éviter le dépassement du SLA.</p><p><a href="{{url}}" style="display:inline-block;background:#123b5d;color:white;text-decoration:none;padding:13px 20px;border-radius:9px;font-weight:bold">Consulter l’incident</a></p></div></div>""";
                await channel.SendEmailAsync(manager.Email,subject,html,ct);
            }
        }
        db.AuditLogs.Add(new AuditLog(null,type,"Incident",incident.Id,$"Manager(s) notifié(s) : {managers.Length}; temps restant : {remainingText}",null));
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
        }
        db.AuditLogs.Add(new AuditLog(null,type,"Incident",incident.Id,$"Rôle notifié : {role}; destinataires : {users.Length}",null));
    }

    private static(int Supervisor,int Manager)Delays(IncidentPriority priority)=>priority switch
    {
        IncidentPriority.P1Critical=>(5,15),IncidentPriority.P2High=>(15,30),IncidentPriority.P3Medium=>(30,60),_=>(60,120)
    };
}
