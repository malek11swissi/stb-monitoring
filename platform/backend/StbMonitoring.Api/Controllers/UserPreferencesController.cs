// API des préférences de notification et filtres sauvegardés par utilisateur.
using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using StbMonitoring.Domain.Entities;using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/user-preferences"),Authorize]public sealed class UserPreferencesController(MonitoringDbContext db):ControllerBase
{
 [HttpGet("notifications")]public async Task<IActionResult>Get(CancellationToken ct){var id=Actor();return Ok(await db.UserNotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==id,ct)??new UserNotificationPreference(id));}
 [HttpPut("notifications")]public async Task<IActionResult>Put(NotificationPreferenceRequest r,CancellationToken ct){var id=Actor();var x=await db.UserNotificationPreferences.FindAsync([id],ct);if(x is null){x=new(id);db.Add(x);}x.Update(r.InAppEnabled,r.EmailEnabled,r.SmsEnabled,r.PhoneNumber,r.CriticalOnly,r.EscalationDelayMinutes);await db.SaveChangesAsync(ct);return Ok(x);}
 [HttpGet("views")]public async Task<IActionResult>Views([FromQuery]string? scope,CancellationToken ct)=>Ok(await db.SavedViews.AsNoTracking().Where(x=>x.UserId==Actor()&&(scope==null||x.Scope==scope)).OrderBy(x=>x.Name).ToArrayAsync(ct));
 [HttpPost("views")]public async Task<IActionResult>CreateView(SavedViewRequest r,CancellationToken ct){var x=new SavedView(Actor(),r.Name,r.Scope,r.FiltersJson);db.Add(x);await db.SaveChangesAsync(ct);return Ok(x);}
 [HttpPut("views/{id:guid}")]public async Task<IActionResult>UpdateView(Guid id,SavedViewRequest r,CancellationToken ct){var x=await db.SavedViews.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==Actor(),ct);if(x is null)return NotFound();x.Update(r.Name,r.Scope,r.FiltersJson);await db.SaveChangesAsync(ct);return Ok(x);}
 [HttpDelete("views/{id:guid}")]public async Task<IActionResult>DeleteView(Guid id,CancellationToken ct){var x=await db.SavedViews.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==Actor(),ct);if(x is null)return NotFound();db.Remove(x);await db.SaveChangesAsync(ct);return NoContent();}
 Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);public sealed record NotificationPreferenceRequest(bool InAppEnabled,bool EmailEnabled,bool SmsEnabled,string? PhoneNumber,bool CriticalOnly,int EscalationDelayMinutes);public sealed record SavedViewRequest(string Name,string Scope,string FiltersJson);
}
