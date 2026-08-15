using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using StbMonitoring.Application.Contracts;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Constants;using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/permissions"),Authorize]
public sealed class PermissionsController(IIdentityStore store):ControllerBase
{
 [HttpGet,Authorize(Policy=PermissionNames.RolesRead)]public async Task<IActionResult> All(CancellationToken ct)=>Ok((await store.GetPermissionsAsync(ct)).Select(Map));
 [HttpPost,Authorize(Policy=PermissionNames.RolesManage)]public async Task<IActionResult> Create(CreatePermissionRequest r,CancellationToken ct){if(await store.FindPermissionByNameAsync(r.Name,ct)is not null)return Conflict(new{message="Cette permission existe déjà."});var p=new Permission(r.Name,r.Description);store.AddPermission(p);Audit("PERMISSION_CREATED",p.Id);await store.SaveChangesAsync(ct);return Created("/api/permissions",Map(p));}
 [HttpPut("{id:guid}"),Authorize(Policy=PermissionNames.RolesManage)]public async Task<IActionResult> Update(Guid id,UpdatePermissionRequest r,CancellationToken ct){var permission=await store.FindPermissionByIdAsync(id,ct);if(permission is null)return NotFound();var duplicate=await store.FindPermissionByNameAsync(r.Name,ct);if(duplicate is not null&&duplicate.Id!=id)return Conflict(new{message="Cette permission existe déjà."});permission.Update(r.Name,r.Description);Audit("PERMISSION_UPDATED",id);await store.SaveChangesAsync(ct);return Ok(Map(permission));}
 [HttpDelete("{id:guid}"),Authorize(Policy=PermissionNames.RolesManage)]public async Task<IActionResult> Delete(Guid id,CancellationToken ct){var permission=await store.FindPermissionByIdAsync(id,ct);if(permission is null)return NotFound();foreach(var link in permission.RolePermissions.ToArray())store.RemoveRolePermission(link);store.RemovePermission(permission);Audit("PERMISSION_DELETED",id);await store.SaveChangesAsync(ct);return NoContent();}
 private static PermissionResponse Map(Permission x)=>new(x.Id,x.Name,x.Description);
 private void Audit(string action,Guid id)=>store.AddAudit(new AuditLog(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),action,"Permission",id,null,HttpContext.Connection.RemoteIpAddress?.ToString()));
}
