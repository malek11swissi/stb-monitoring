// API d'administration des comptes, protégée par les droits utilisateurs.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/users"),Authorize]
public sealed class UsersController(IUserService users,StbMonitoring.Infrastructure.Persistence.MonitoringDbContext db):ControllerBase
{
    [HttpGet,Authorize(Policy=PermissionNames.UsersRead)] public async Task<IActionResult> All(CancellationToken ct)=>Ok(await users.GetAllAsync(ct));
    [HttpGet("{id:guid}"),Authorize(Policy=PermissionNames.UsersRead)] public async Task<IActionResult> One(Guid id,CancellationToken ct){var x=await users.GetByIdAsync(id,ct);return x is null?NotFound():Ok(x);}
    [HttpPost,Authorize(Policy=PermissionNames.UsersManage)] public async Task<IActionResult> Create(CreateUserRequest r,CancellationToken ct){var x=await users.CreateAsync(r,Actor(),Ip(),ct);await NotifyAdmins("USER_CREATED","Compte utilisateur créé",$"{x.FirstName} {x.LastName} · {x.Role}",x.Id,ct);return CreatedAtAction(nameof(One),new{id=x.Id},x);}
    [HttpPut("{id:guid}"),Authorize(Policy=PermissionNames.UsersManage)] public async Task<IActionResult> Update(Guid id,UpdateUserRequest r,CancellationToken ct){var x=await users.UpdateAsync(id,r,Actor(),Ip(),ct);await NotifyAdmins("USER_UPDATED","Compte utilisateur modifié",$"{x.FirstName} {x.LastName} · {x.Role}",x.Id,ct);return Ok(x);}
    [HttpPatch("{id:guid}/active"),Authorize(Policy=PermissionNames.UsersManage)] public async Task<IActionResult> Active(Guid id,[FromQuery]bool value,CancellationToken ct){await users.SetActiveAsync(id,value,Actor(),Ip(),ct);var target=await users.GetByIdAsync(id,ct);await NotifyAdmins(value?"USER_ACTIVATED":"USER_DEACTIVATED",value?"Compte réactivé":"Compte désactivé",target is null?id.ToString():$"{target.FirstName} {target.LastName}",id,ct);return NoContent();}
    [HttpGet("{id:guid}/deletion-eligibility"),Authorize(Policy=PermissionNames.UsersManage)]
    public async Task<IActionResult> DeletionEligibility(Guid id,CancellationToken ct)
    {
        var target=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(target is null)return NotFound();
        var result=await GetDeletionEligibility(id,target.Role,ct);
        return Ok(new{result.CanDelete,result.Reason});
    }
    [HttpDelete("{id:guid}"),Authorize(Policy=PermissionNames.UsersManage)]
    public async Task<IActionResult> DeletePermanently(Guid id,CancellationToken ct)
    {
        var target=await db.Users.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(target is null)return NotFound();
        var result=await GetDeletionEligibility(id,target.Role,ct);
        if(!result.CanDelete)return Conflict(new{message=result.Reason});

        // Les notifications reçues ne constituent pas une activité de l'utilisateur
        // et sont supprimées avec le compte pour éviter toute donnée orpheline.
        await db.Notifications.Where(x=>x.UserId==id).ExecuteDeleteAsync(ct);
        db.Users.Remove(target);
        db.AuditLogs.Add(new(Actor(),"USER_PERMANENTLY_DELETED","User",id,$"Compte {target.Username} supprimé définitivement",Ip(),true));
        foreach(var adminId in await db.Users.Where(x=>x.IsActive&&x.Role==RoleNames.Admin&&x.Id!=id).Select(x=>x.Id).ToArrayAsync(ct))
            db.Notifications.Add(new(adminId,"USER_DELETED","Compte supprimé définitivement",$"Le compte @{target.Username} a été supprimé.",StbMonitoring.Domain.Entities.AlertSeverity.Warning,"User",id,"/users"));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("technicians"),Authorize(Policy=PermissionNames.IncidentsAssign)]
    public async Task<IActionResult> Technicians(CancellationToken ct)=>Ok(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToArrayAsync(db.Users.AsNoTracking().Where(x=>x.IsActive&&x.Role==RoleNames.Technician).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).Select(x=>new{x.Id,x.FirstName,x.LastName,x.Role,x.AvatarPath}),ct));
    private async Task<(bool CanDelete,string Reason)> GetDeletionEligibility(Guid id,string role,CancellationToken ct)
    {
        if(id==Actor())return(false,"Vous ne pouvez pas supprimer définitivement votre propre compte.");
        if(role==RoleNames.Admin&&await db.Users.CountAsync(x=>x.Role==RoleNames.Admin&&x.IsActive,ct)<=1)
            return(false,"Le dernier compte administrateur actif doit être conservé.");

        // Une activité métier reste attribuée à son auteur afin de préserver la
        // traçabilité. Dans ce cas le compte peut être désactivé, mais pas supprimé.
        var hasActivity=
            await db.AuditLogs.AnyAsync(x=>x.UserId==id,ct)||
            await db.CheckResults.AnyAsync(x=>x.TriggeredByUserId==id,ct)||
            await db.Alerts.AnyAsync(x=>x.AcknowledgedByUserId==id||x.ClosedByUserId==id,ct)||
            await db.Incidents.AnyAsync(x=>x.AssignedToUserId==id||x.AssignedByUserId==id||x.CreatedByUserId==id||x.ClosedByUserId==id||x.CancelledByUserId==id||x.ArchivedByUserId==id,ct)||
            await db.IncidentComments.AnyAsync(x=>x.UserId==id,ct)||
            await db.IncidentHistories.AnyAsync(x=>x.UserId==id,ct)||
            await db.IncidentAttachments.AnyAsync(x=>x.UploadedByUserId==id,ct)||
            await db.MaintenanceWindows.AnyAsync(x=>x.CreatedByUserId==id,ct);
        return hasActivity
            ?(false,"Ce compte possède une activité. Désactivez-le pour conserver la traçabilité.")
            :(true,"Ce compte ne possède aucune activité et peut être supprimé définitivement.");
    }
    private async Task NotifyAdmins(string type,string title,string message,Guid userId,CancellationToken ct)
    {
        foreach(var adminId in await db.Users.Where(x=>x.IsActive&&x.Role==RoleNames.Admin).Select(x=>x.Id).ToArrayAsync(ct))
            db.Notifications.Add(new(adminId,type,title,message,StbMonitoring.Domain.Entities.AlertSeverity.Info,"User",userId,"/users"));
        await db.SaveChangesAsync(ct);
    }
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
}
