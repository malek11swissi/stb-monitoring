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

    [HttpPost, Authorize(Policy = PermissionNames.IncidentsManage)]
    public async Task<IActionResult> Create(CreateIncidentRequest request, CancellationToken ct) =>
        Ok(await service.CreateIncidentAsync(request, Actor(), ct));

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

    [HttpPost("{id:guid}/comments"), Authorize(Policy = PermissionNames.IncidentsWork), Authorize(Roles = RoleNames.Technician)]
    public async Task<IActionResult> Comment(Guid id, AddCommentRequest request, CancellationToken ct)
    { if (await TechnicianAccess(id, ct) is { } denied) return denied; await service.CommentAsync(id, request, Actor(), ct); return NoContent(); }

    [HttpGet("{id:guid}/attachments"), Authorize(Policy = PermissionNames.IncidentsRead)]
    public async Task<IActionResult> Attachments(Guid id,CancellationToken ct){if(await TechnicianAccess(id,ct)is{}denied)return denied;return Ok(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToArrayAsync(db.IncidentAttachments.AsNoTracking().Where(x=>x.IncidentId==id).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.FileName,x.ContentType,size=x.Content.LongLength,x.IsResolutionProof,x.CreatedAt}),ct));}
    [HttpPost("{id:guid}/attachments"),Authorize(Policy=PermissionNames.IncidentsWork),Authorize(Roles=RoleNames.Technician),RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> Upload(Guid id,IFormFile file,[FromForm]bool isResolutionProof,CancellationToken ct){if(await TechnicianAccess(id,ct)is{}denied)return denied;if(file.Length is 0 or >10_485_760)return BadRequest(new{message="Fichier vide ou supérieur à 10 Mo."});await using var ms=new MemoryStream();await file.CopyToAsync(ms,ct);var x=new StbMonitoring.Domain.Entities.IncidentAttachment(id,Actor(),file.FileName,file.ContentType,ms.ToArray(),isResolutionProof);db.Add(x);await db.SaveChangesAsync(ct);return Ok(new{x.Id,x.FileName,x.ContentType,size=x.Content.LongLength,x.IsResolutionProof,x.CreatedAt});}
    [HttpGet("attachments/{attachmentId:guid}/download"),Authorize(Policy=PermissionNames.IncidentsRead)]
    public async Task<IActionResult> Download(Guid attachmentId,CancellationToken ct){var x=await db.IncidentAttachments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==attachmentId,ct);if(x is null)return NotFound();if(await TechnicianAccess(x.IncidentId,ct)is{}denied)return denied;return File(x.Content,x.ContentType,x.FileName);}
    [HttpDelete("attachments/{attachmentId:guid}"),Authorize(Policy=PermissionNames.IncidentsManage)]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId,CancellationToken ct){var x=await db.IncidentAttachments.FindAsync([attachmentId],ct);if(x is null)return NotFound();db.Remove(x);await db.SaveChangesAsync(ct);return NoContent();}

    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<IActionResult?> TechnicianAccess(Guid id, CancellationToken ct)
    {
        if (!User.IsInRole(RoleNames.Technician)) return null;
        var detail = await service.IncidentAsync(id, ct);
        if (detail is null) return NotFound();
        return detail.Incident.AssignedToUserId == Actor() ? null : Forbid();
    }
}
