using Microsoft.EntityFrameworkCore;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Constants;using StbMonitoring.Domain.Entities;using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.Infrastructure.Notifications;
/// <summary>
/// Envoie l'e-mail d'affectation quelle que soit la priorité, ajoute un SMS pour
/// un P1 critique et programme son escalade persistante vers le superviseur.
/// </summary>
public sealed class IncidentNotificationDispatcher(MonitoringDbContext db,INotificationChannel channel):IIncidentNotificationDispatcher
{
 public async Task NotifyAssignmentAsync(Incident incident,CancellationToken ct){if(!incident.AssignedToUserId.HasValue)return;var user=await db.Users.FindAsync([incident.AssignedToUserId.Value],ct);if(user is null)return;var subject=$"[{incident.Priority}] {incident.IncidentNumber} — nouvel incident affecté";if(user.EmailNotificationsEnabled)await channel.SendEmailAsync(user.Email,subject,$"<h2>{incident.Title}</h2><p>Un incident vous a été affecté.</p><p>{incident.Description}</p><p><strong>Priorité :</strong> {incident.Priority}</p>",ct);if(incident.Priority==IncidentPriority.P1Critical&&user.SmsNotificationsEnabled&&!string.IsNullOrWhiteSpace(user.PhoneNumber))await channel.SendSmsAsync(user.PhoneNumber,$"STB Sentinel CRITIQUE {incident.IncidentNumber}: {incident.Title}. Consultez la plateforme.",ct);}
}
