// API du calendrier et des fenêtres qui suspendent temporairement les alertes.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;

namespace StbMonitoring.Api.Controllers;

[ApiController, Route("api/maintenance-windows"), Authorize(Policy = PermissionNames.MaintenanceRead)]
public sealed class MaintenanceWindowsController(MonitoringDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var start=from?.ToUniversalTime()??DateTime.UtcNow.AddMonths(-1);var end=to?.ToUniversalTime()??DateTime.UtcNow.AddMonths(3);
        return Ok(await db.MaintenanceWindows.AsNoTracking().Where(x=>!x.IsCancelled&&x.StartsAt<=end&&x.EndsAt>=start).OrderBy(x=>x.StartsAt).ToArrayAsync(ct));
    }
    [HttpGet("active")]
    public async Task<IActionResult> Active([FromQuery]Guid? systemId,[FromQuery]Guid? endpointId,CancellationToken ct){var now=DateTime.UtcNow;return Ok(await db.MaintenanceWindows.AsNoTracking().Where(x=>!x.IsCancelled&&x.StartsAt<=now&&x.EndsAt>=now&&(!systemId.HasValue||x.SystemId==systemId)&&(!endpointId.HasValue||x.EndpointId==endpointId)).ToArrayAsync(ct));}
    [HttpPost,Authorize(Policy=PermissionNames.MaintenanceManage)]
    public async Task<IActionResult>Create(MaintenanceRequest r,CancellationToken ct){var x=new MaintenanceWindow(r.Title,r.Description,r.SystemId,r.EndpointId,r.StartsAt,r.EndsAt,r.SuppressAlerts,Actor());db.Add(x);await db.SaveChangesAsync(ct);return CreatedAtAction(nameof(All),new{id=x.Id},x);}
    [HttpPut("{id:guid}"),Authorize(Policy=PermissionNames.MaintenanceManage)]
    public async Task<IActionResult>Update(Guid id,MaintenanceRequest r,CancellationToken ct){var x=await db.MaintenanceWindows.FindAsync([id],ct)??throw new KeyNotFoundException("Maintenance introuvable.");x.Update(r.Title,r.Description,r.SystemId,r.EndpointId,r.StartsAt,r.EndsAt,r.SuppressAlerts);await db.SaveChangesAsync(ct);return Ok(x);}
    [HttpPost("{id:guid}/cancel"),Authorize(Policy=PermissionNames.MaintenanceManage)]
    public async Task<IActionResult>Cancel(Guid id,CancellationToken ct){var x=await db.MaintenanceWindows.FindAsync([id],ct)??throw new KeyNotFoundException("Maintenance introuvable.");x.Cancel();await db.SaveChangesAsync(ct);return NoContent();}
    [HttpDelete("{id:guid}"),Authorize(Policy=PermissionNames.MaintenanceManage)]
    public async Task<IActionResult>Delete(Guid id,CancellationToken ct){var x=await db.MaintenanceWindows.FindAsync([id],ct)??throw new KeyNotFoundException("Maintenance introuvable.");db.MaintenanceWindows.Remove(x);await db.SaveChangesAsync(ct);return NoContent();}
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public sealed record MaintenanceRequest(string Title,string? Description,Guid? SystemId,Guid? EndpointId,DateTime StartsAt,DateTime EndsAt,bool SuppressAlerts=true);
}
