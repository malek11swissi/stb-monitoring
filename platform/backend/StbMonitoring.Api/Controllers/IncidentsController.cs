// API du cycle complet d'incident, affectation, commentaires et preuves.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Api.Controllers;

[ApiController, Route("api/incidents"), Authorize]
public sealed class IncidentsController(IOperationsService service,StbMonitoring.Infrastructure.Persistence.MonitoringDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Policy = PermissionNames.IncidentsRead)]
    public async Task<IActionResult> All([FromQuery]bool archived=false,CancellationToken ct=default)
    {
        var incidents = await service.IncidentsAsync(ct);
        incidents=incidents.Where(x=>x.IsArchived==archived).ToArray();
        return Ok(User.IsInRole(RoleNames.Technician)
            ? incidents.Where(x => x.AssignedToUserId == Actor())
            : incidents);
    }

    [HttpGet("paged"),Authorize(Policy=PermissionNames.IncidentsRead)]
    public async Task<IActionResult>Paged([FromQuery]int page=1,[FromQuery]int pageSize=15,[FromQuery]string? search=null,[FromQuery]IncidentStatus? status=null,[FromQuery]IncidentPriority? priority=null,[FromQuery]SlaStatus? sla=null,[FromQuery]string? assignment=null,[FromQuery]bool archived=false,CancellationToken ct=default)
    {page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,10,100);var q=db.Incidents.AsNoTracking().Where(x=>x.IsArchived==archived);if(User.IsInRole(RoleNames.Technician)){var actor=Actor();q=q.Where(x=>x.AssignedToUserId==actor);}var today=DateTime.UtcNow.Date;var todayTotal=await q.CountAsync(x=>x.CreatedAt>=today,ct);if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim().ToLower();q=q.Where(x=>x.IncidentNumber.ToLower().Contains(s)||x.Title.ToLower().Contains(s));}if(status.HasValue)q=q.Where(x=>x.Status==status);if(priority.HasValue)q=q.Where(x=>x.Priority==priority);if(sla.HasValue)q=q.Where(x=>x.SlaStatus==sla);if(assignment=="assigned")q=q.Where(x=>x.AssignedToUserId!=null);if(assignment=="unassigned")q=q.Where(x=>x.AssignedToUserId==null);var total=await q.CountAsync(ct);var ids=await q.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>x.Id).ToArrayAsync(ct);var all=await service.IncidentsAsync(ct);var items=ids.Select(id=>all.Single(x=>x.Id==id)).ToArray();return Ok(new{items,total,todayTotal,page,pageSize,totalPages=(int)Math.Ceiling(total/(double)pageSize)});}

    [HttpGet("{id:guid}"), Authorize(Policy = PermissionNames.IncidentsRead)]
    public async Task<IActionResult> One(Guid id, CancellationToken ct)
    {
        var incident = await service.IncidentAsync(id, ct);
        if (incident is null) return NotFound();
        if (User.IsInRole(RoleNames.Technician) && incident.Incident.AssignedToUserId != Actor()) return Forbid();
        return Ok(incident);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = PermissionNames.IncidentsManage)]
    public async Task<IActionResult> Update(Guid id, UpdateIncidentRequest request, CancellationToken ct) =>
        Ok(await service.UpdateIncidentAsync(id, request, Actor(), ct));

    [HttpPost("{id:guid}/assign"), Authorize(Policy = PermissionNames.IncidentsAssign)]
    public async Task<IActionResult> Assign(Guid id, AssignIncidentRequest request, CancellationToken ct)
    { await service.AssignAsync(id, request.UserId, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/start"), Authorize(Policy = PermissionNames.IncidentsWork), Authorize(Roles = RoleNames.Technician)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    { if (await TechnicianAccess(id, ct) is { } denied) return denied; await service.StartAsync(id, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/pending"), Authorize(Policy = PermissionNames.IncidentsWork), Authorize(Roles = RoleNames.Technician)]
    public async Task<IActionResult> Pending(Guid id, CancellationToken ct)
    { if (await TechnicianAccess(id, ct) is { } denied) return denied; await service.PendingAsync(id, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/resolve"), Authorize(Policy = PermissionNames.IncidentsResolve), Authorize(Roles = RoleNames.Technician)]
    public async Task<IActionResult> Resolve(Guid id, ResolveIncidentRequest request, CancellationToken ct)
    { if (await TechnicianAccess(id, ct) is { } denied) return denied; await service.ResolveIncidentAsync(id, request, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/close"), Authorize(Policy = PermissionNames.IncidentsClose)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    { await service.CloseIncidentAsync(id, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/reopen"), Authorize(Policy = PermissionNames.IncidentsClose)]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    { await service.ReopenAsync(id, Actor(), ct); return NoContent(); }

    [HttpPost("{id:guid}/cancel"),Authorize(Policy=PermissionNames.IncidentsArchive)]
    public async Task<IActionResult> Cancel(Guid id,IncidentActionReasonRequest request,CancellationToken ct){await service.CancelIncidentAsync(id,request.Reason,Actor(),ct);return NoContent();}

    [HttpPost("{id:guid}/archive"),Authorize(Policy=PermissionNames.IncidentsArchive)]
    public async Task<IActionResult> Archive(Guid id,CancellationToken ct){await service.ArchiveIncidentAsync(id,Actor(),ct);return NoContent();}

    [HttpPost("{id:guid}/restore"),Authorize(Policy=PermissionNames.IncidentsArchive)]
    public async Task<IActionResult> Restore(Guid id,CancellationToken ct){await service.RestoreIncidentAsync(id,Actor(),ct);return NoContent();}

    [HttpDelete("{id:guid}"),Authorize(Policy=PermissionNames.IncidentsDelete)]
    public async Task<IActionResult> Delete(Guid id,[FromBody]IncidentActionReasonRequest request,CancellationToken ct){await service.DeleteIncidentAsync(id,request.Reason,Actor(),ct);return NoContent();}

    [HttpPost("{id:guid}/comments"), Authorize(Roles = RoleNames.Supervisor+","+RoleNames.Technician+","+RoleNames.ManagerIt)]
    public async Task<IActionResult> Comment(Guid id, AddCommentRequest request, CancellationToken ct)
    { if (await TechnicianAccess(id, ct) is { } denied) return denied; await service.CommentAsync(id, request, Actor(), ct); return NoContent(); }

    [HttpGet("{id:guid}/timeline"),Authorize(Policy=PermissionNames.IncidentsRead)]
    public async Task<IActionResult> Timeline(Guid id,CancellationToken ct)
    {
        if(await TechnicianAccess(id,ct)is{}denied)return denied;
        var comments=await (from c in db.IncidentComments.AsNoTracking() join u in db.Users.AsNoTracking() on c.UserId equals u.Id where c.IncidentId==id select new IncidentTimelineItemResponse(c.Id,"comment",c.UserId,u.FirstName+" "+u.LastName,string.IsNullOrEmpty(u.AvatarPath)?null:"/"+u.AvatarPath.Replace("\\","/").TrimStart('/'),"Commentaire",c.Content,null,null,c.CreatedAt)).ToArrayAsync(ct);
        var history=await (from h in db.IncidentHistories.AsNoTracking() join u0 in db.Users.AsNoTracking() on h.UserId equals u0.Id into users from u in users.DefaultIfEmpty() where h.IncidentId==id select new IncidentTimelineItemResponse(h.Id,"history",h.UserId,u==null?"STB Sentinel":u.FirstName+" "+u.LastName,u==null||string.IsNullOrEmpty(u.AvatarPath)?null:"/"+u.AvatarPath.Replace("\\","/").TrimStart('/'),h.Action,h.Details,h.OldValue,h.NewValue,h.CreatedAt)).ToArrayAsync(ct);
        // Les commentaires ont leur propre section. La chronologie ne contient
        // que les changements métier afin d'éviter une double représentation.
        return Ok(history.OrderByDescending(x=>x.CreatedAt));
    }

    [HttpGet("{id:guid}/attachments"), Authorize(Policy = PermissionNames.IncidentsRead)]
    public async Task<IActionResult> Attachments(Guid id,CancellationToken ct){if(await TechnicianAccess(id,ct)is{}denied)return denied;return Ok(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToArrayAsync(db.IncidentAttachments.AsNoTracking().Where(x=>x.IncidentId==id).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.FileName,x.ContentType,size=x.Content.LongLength,x.IsResolutionProof,x.CreatedAt}),ct));}
    [HttpPost("{id:guid}/attachments"),Authorize(Policy=PermissionNames.IncidentsWork),Authorize(Roles=RoleNames.Technician),RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> Upload(Guid id,IFormFile file,[FromForm]bool isResolutionProof,CancellationToken ct)
    {
        if(await TechnicianAccess(id,ct)is{}denied)return denied;
        if(file.Length is 0 or >10_485_760)return BadRequest(new{message="Fichier vide ou supérieur à 10 Mo."});
        var safeName=Path.GetFileName(file.FileName);if(string.IsNullOrWhiteSpace(safeName)||safeName.Length>180)return BadRequest(new{message="Nom de fichier invalide ou trop long."});
        await using var ms=new MemoryStream();await file.CopyToAsync(ms,ct);var content=ms.ToArray();
        var validation=ValidateAttachment(safeName,file.ContentType,content);if(!validation.Valid)return BadRequest(new{message=validation.Error});
        var x=new IncidentAttachment(id,Actor(),safeName,validation.ContentType,content,isResolutionProof);db.Add(x);db.AuditLogs.Add(new(Actor(),"INCIDENT_ATTACHMENT_ADDED","Incident",id,$"{safeName}; {content.Length} octets; preuve={isResolutionProof}",HttpContext.Connection.RemoteIpAddress?.ToString()));await db.SaveChangesAsync(ct);return Ok(new{x.Id,x.FileName,x.ContentType,size=x.Content.LongLength,x.IsResolutionProof,x.CreatedAt});
    }
    [HttpGet("attachments/{attachmentId:guid}/download"),Authorize(Policy=PermissionNames.IncidentsRead)]
    public async Task<IActionResult> Download(Guid attachmentId,CancellationToken ct){var x=await db.IncidentAttachments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==attachmentId,ct);if(x is null)return NotFound();if(await TechnicianAccess(x.IncidentId,ct)is{}denied)return denied;return File(x.Content,x.ContentType,x.FileName);}
    [HttpDelete("attachments/{attachmentId:guid}"),Authorize(Roles=RoleNames.Technician)]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId,CancellationToken ct){var x=await db.IncidentAttachments.FindAsync([attachmentId],ct);if(x is null)return NotFound();if(x.UploadedByUserId!=Actor())return Forbid();if(await TechnicianAccess(x.IncidentId,ct)is{}denied)return denied;var incident=await db.Incidents.AsNoTracking().SingleAsync(i=>i.Id==x.IncidentId,ct);if(incident.Status is IncidentStatus.Resolved or IncidentStatus.Closed)return Conflict(new{message="Une preuve d'un incident résolu ne peut plus être supprimée."});db.Remove(x);await db.SaveChangesAsync(ct);return NoContent();}

    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static (bool Valid,string ContentType,string? Error) ValidateAttachment(string name,string declared,byte[] content)
    {
        var extension=Path.GetExtension(name).ToLowerInvariant();
        var binary=new Dictionary<string,(string Mime,byte[] Signature)>{[".pdf"]=("application/pdf",[0x25,0x50,0x44,0x46]),[".jpg"]=("image/jpeg",[0xFF,0xD8,0xFF]),[".jpeg"]=("image/jpeg",[0xFF,0xD8,0xFF]),[".png"]=("image/png",[0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A]),[".webp"]=("image/webp",[0x52,0x49,0x46,0x46])};
        if(binary.TryGetValue(extension,out var format))return content.AsSpan().StartsWith(format.Signature)?(true,format.Mime,null):(false,"",$"Le contenu ne correspond pas à un fichier {extension} valide.");
        if(extension is ".txt" or ".log" or ".csv" or ".json")
        {
            if(content.Contains((byte)0))return(false,"","Le fichier texte contient des données binaires interdites.");
            try{_ = new System.Text.UTF8Encoding(false,true).GetString(content);return(true,extension==".json"?"application/json":extension==".csv"?"text/csv":"text/plain",null);}catch{return(false,"","Le fichier texte doit être encodé en UTF-8.");}
        }
        return(false,"",$"Type non autorisé. Formats acceptés : PDF, JPG, PNG, WebP, TXT, LOG, CSV et JSON. Type déclaré : {declared}");
    }
    private async Task<IActionResult?> TechnicianAccess(Guid id, CancellationToken ct)
    {
        if (!User.IsInRole(RoleNames.Technician)) return null;
        var detail = await service.IncidentAsync(id, ct);
        if (detail is null) return NotFound();
        return detail.Incident.AssignedToUserId == Actor() ? null : Forbid();
    }
}
