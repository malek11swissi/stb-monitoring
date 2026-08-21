using Microsoft.EntityFrameworkCore;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Constants;using StbMonitoring.Domain.Entities;using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.Infrastructure.Notifications;
/// <summary>
/// Lance les notifications externes uniquement pour un incident P1 critique.
/// L'escalade est enregistrée en base afin de survivre aux redémarrages de l'API.
/// </summary>
public sealed class IncidentNotificationDispatcher(MonitoringDbContext db,INotificationChannel channel):IIncidentNotificationDispatcher
{
 public async Task DispatchCriticalAsync(Incident incident,string stage,CancellationToken ct){if(incident.Priority!=IncidentPriority.P1Critical)return;if(incident.AssignedToUserId.HasValue){var user=await db.Users.FindAsync([incident.AssignedToUserId.Value],ct);if(user is not null)await Send(user,incident,stage,ct);}if(!await db.IncidentEscalations.AnyAsync(x=>x.IncidentId==incident.Id,ct)){var delay=await db.UserNotificationPreferences.Where(x=>x.UserId==incident.AssignedToUserId).Select(x=>(int?)x.EscalationDelayMinutes).SingleOrDefaultAsync(ct)??15;db.Add(new IncidentEscalation(incident.Id,delay));await db.SaveChangesAsync(ct);}}
 private async Task Send(User user,Incident incident,string stage,CancellationToken ct){var p=await db.UserNotificationPreferences.FindAsync([user.Id],ct);var subject=$"[CRITIQUE] {incident.IncidentNumber} — {incident.Title}";if(p?.EmailEnabled!=false)await channel.SendEmailAsync(user.Email,subject,$"<h2>{subject}</h2><p>{stage}</p><p>{incident.Description}</p>",ct);if(p?.SmsEnabled==true&&!string.IsNullOrWhiteSpace(p.PhoneNumber))await channel.SendSmsAsync(p.PhoneNumber,$"STB Sentinel {subject}. {stage}",ct);}
}
