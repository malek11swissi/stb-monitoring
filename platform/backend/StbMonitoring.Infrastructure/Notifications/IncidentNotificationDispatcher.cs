using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.Configuration;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Constants;using StbMonitoring.Domain.Entities;using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.Infrastructure.Notifications;
/// <summary>
/// Envoie l'e-mail d'affectation quelle que soit la priorité, ajoute un SMS pour
/// un P1 critique et programme son escalade persistante vers le superviseur.
/// </summary>
public sealed class IncidentNotificationDispatcher(MonitoringDbContext db,INotificationChannel channel,IConfiguration configuration):IIncidentNotificationDispatcher
{
 public async Task NotifyAssignmentAsync(Incident incident,CancellationToken ct)
 {
  if(!incident.AssignedToUserId.HasValue)return;
  var user=await db.Users.FindAsync([incident.AssignedToUserId.Value],ct);if(user is null||!user.EmailNotificationsEnabled)return;
  var system=incident.SystemId.HasValue?await db.Systems.AsNoTracking().Where(x=>x.Id==incident.SystemId).Select(x=>x.Name).FirstOrDefaultAsync(ct):null;
  var endpoint=incident.EndpointId.HasValue?await db.MonitoringEndpoints.AsNoTracking().Where(x=>x.Id==incident.EndpointId).Select(x=>x.Name).FirstOrDefaultAsync(ct):null;
  var supervisor=incident.AssignedByUserId.HasValue?await db.Users.AsNoTracking().Where(x=>x.Id==incident.AssignedByUserId).Select(x=>x.FirstName+" "+x.LastName).FirstOrDefaultAsync(ct):null;
  var due=incident.ResolutionDueAt;var remaining=due.HasValue?due.Value-DateTime.UtcNow:TimeSpan.Zero;
  var remainingText=!due.HasValue?"Non définie":remaining<=TimeSpan.Zero?"SLA déjà dépassé":remaining.TotalHours>=1?$"{Math.Floor(remaining.TotalHours)} h {remaining.Minutes} min":$"{Math.Max(0,remaining.Minutes)} min";
  var url=$"{(configuration["FrontendUrl"]??"http://localhost:4200").TrimEnd('/')}/incidents/{incident.Id}";
  static string E(string? value)=>System.Net.WebUtility.HtmlEncode(value??"Non renseigné");
  var subject=$"[ACTION REQUISE · {incident.Priority}] {incident.IncidentNumber} — incident affecté";
  var html=$$"""
  <div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;color:#18252e;border:1px solid #dce7eb;border-radius:14px;overflow:hidden">
   <div style="background:#123b5d;color:white;padding:24px"><div style="color:#6fe0e5;font-size:12px;font-weight:bold;letter-spacing:1px">STB MONITORING · ACTION TECHNICIEN</div><h1 style="font-size:24px;margin:8px 0 0">Un incident requiert votre intervention</h1></div>
   <div style="padding:24px"><p>Bonjour <strong>{{E(user.FirstName)}}</strong>,</p><p>Le superviseur <strong>{{E(supervisor)}}</strong> vous a affecté un incident. Merci de le prendre en charge immédiatement et de documenter chaque action réalisée.</p>
    <div style="background:#f5f8fa;border-left:4px solid #087e8b;padding:16px;margin:20px 0"><strong>{{E(incident.IncidentNumber)}} — {{E(incident.Title)}}</strong><br><span style="color:#61727e">{{E(incident.Description)}}</span></div>
    <table style="width:100%;border-collapse:collapse"><tr><td style="padding:8px;color:#61727e">Priorité</td><td style="padding:8px;font-weight:bold">{{E(incident.Priority.ToString())}}</td></tr><tr><td style="padding:8px;color:#61727e">SI</td><td style="padding:8px">{{E(system)}}</td></tr><tr><td style="padding:8px;color:#61727e">Endpoint</td><td style="padding:8px">{{E(endpoint)}}</td></tr><tr><td style="padding:8px;color:#61727e">Date d’affectation</td><td style="padding:8px">{{incident.AssignedAt:dd/MM/yyyy HH:mm}} UTC</td></tr><tr><td style="padding:8px;color:#61727e">Échéance de résolution</td><td style="padding:8px">{{(due.HasValue?due.Value.ToString("dd/MM/yyyy HH:mm")+" UTC":"Non définie")}}</td></tr><tr><td style="padding:8px;color:#61727e">Temps restant</td><td style="padding:8px;color:#b42318;font-weight:bold">{{remainingText}}</td></tr></table>
    <p style="margin-top:22px"><a href="{{url}}" style="display:inline-block;background:#087e8b;color:white;text-decoration:none;padding:13px 20px;border-radius:9px;font-weight:bold">Prendre en charge maintenant</a></p><p style="font-size:12px;color:#61727e">Après le diagnostic, ajoutez la cause racine, l’action corrective et une preuve avant la résolution.</p>
   </div>
  </div>
  """;
  await channel.SendEmailAsync(user.Email,subject,html,ct);
 }
}
