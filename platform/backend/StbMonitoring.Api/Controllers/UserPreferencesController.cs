// Les préférences de notification font partie du compte utilisateur. Les vues de filtres restent locales au navigateur.
using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using StbMonitoring.Domain.Entities;using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/user-preferences"),Authorize]public sealed class UserPreferencesController(MonitoringDbContext db):ControllerBase
{
 [HttpGet("notifications")]public async Task<IActionResult>Get(CancellationToken ct){var x=await db.Users.AsNoTracking().SingleAsync(x=>x.Id==Actor(),ct);return Ok(Map(x));}
 [HttpPut("notifications")]public async Task<IActionResult>Put(NotificationPreferenceRequest r,CancellationToken ct){var x=await db.Users.SingleAsync(x=>x.Id==Actor(),ct);x.UpdateNotificationPreferences(r.InAppEnabled,r.EmailEnabled,r.SmsEnabled,r.PhoneNumber,r.CriticalOnly);await db.SaveChangesAsync(ct);return Ok(Map(x));}
 static object Map(User x)=>new{inAppEnabled=x.InAppNotificationsEnabled,emailEnabled=x.EmailNotificationsEnabled,smsEnabled=x.SmsNotificationsEnabled,x.PhoneNumber,criticalOnly=x.CriticalNotificationsOnly};
 Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);public sealed record NotificationPreferenceRequest(bool InAppEnabled,bool EmailEnabled,bool SmsEnabled,string? PhoneNumber,bool CriticalOnly);
}
