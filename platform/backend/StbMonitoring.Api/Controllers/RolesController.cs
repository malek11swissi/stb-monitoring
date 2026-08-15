using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/roles"),Authorize]
public sealed class RolesController(IIdentityStore store):ControllerBase
{
 [HttpGet,Authorize(Policy=PermissionNames.RolesRead)] public async Task<IActionResult> All(CancellationToken ct)=>Ok((await store.GetRolesAsync(ct)).Select(Map));
 [HttpPost,Authorize(Policy=PermissionNames.RolesManage)] public async Task<IActionResult> Create(CreateRoleRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Name))return BadRequest(new{message="Le nom est obligatoire."});if(await store.FindRoleByNameAsync(r.Name,ct)is not null)return Conflict(new{message="Ce rôle existe déjà."});var role=new Role(r.Name,r.Description);store.AddRole(role);Audit("ROLE_CREATED",role.Id);await store.SaveChangesAsync(ct);return Created("/api/roles",Map(role));}
 [HttpPut("{id:guid}"),Authorize(Policy=PermissionNames.RolesManage)] public async Task<IActionResult> Update(Guid id,UpdateRoleRequest r,CancellationToken ct){var role=await store.FindRoleByIdAsync(id,ct);if(role is null)return NotFound();role.Update(r.Name,r.Description);Audit("ROLE_UPDATED",id);await store.SaveChangesAsync(ct);return Ok(Map(role));}
 [HttpPatch("{id:guid}/active"),Authorize(Policy=PermissionNames.RolesManage)] public async Task<IActionResult> Active(Guid id,[FromQuery]bool value,CancellationToken ct){var role=await store.FindRoleByIdAsync(id,ct);if(role is null)return NotFound();if(value)role.Activate();else role.Deactivate();Audit(value?"ROLE_ACTIVATED":"ROLE_ARCHIVED",id);await store.SaveChangesAsync(ct);return NoContent();}
 [HttpDelete("{id:guid}"),Authorize(Policy=PermissionNames.RolesManage)] public async Task<IActionResult> Delete(Guid id,CancellationToken ct){var role=await store.FindRoleByIdAsync(id,ct);if(role is null)return NotFound();if(role.Name==RoleNames.Admin)return Conflict(new{message="Le rôle ADMIN ne peut pas être supprimé."});if(role.UserRoles.Count>0)return Conflict(new{message="Ce rôle est encore attribué à un ou plusieurs utilisateurs."});foreach(var link in role.RolePermissions.ToArray())store.RemoveRolePermission(link);store.RemoveRole(role);Audit("ROLE_DELETED",id);await store.SaveChangesAsync(ct);return NoContent();}
 [HttpPut("{id:guid}/permissions"),Authorize(Policy=PermissionNames.RolesManage)] public async Task<IActionResult> SetPermissions(Guid id,SetRolePermissionsRequest r,CancellationToken ct){var role=await store.FindRoleByIdAsync(id,ct);if(role is null)return NotFound();var desired=new List<Permission>();foreach(var name in r.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))desired.Add(await store.FindPermissionByNameAsync(name,ct)??throw new KeyNotFoundException($"Permission {name} introuvable."));foreach(var link in role.RolePermissions.Where(x=>desired.All(p=>p.Id!=x.PermissionId)).ToArray())store.RemoveRolePermission(link);foreach(var p in desired.Where(p=>role.RolePermissions.All(x=>x.PermissionId!=p.Id)))store.AddRolePermission(new(id,p.Id));Audit("ROLE_PERMISSIONS_CHANGED",id);await store.SaveChangesAsync(ct);return NoContent();}
 private static RoleResponse Map(Role x)=>new(x.Id,x.Name,x.Description,x.IsActive,x.RolePermissions.Select(p=>p.Permission.Name).Order().ToArray());
 private void Audit(string action,Guid id)=>store.AddAudit(new AuditLog(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),action,"Role",id,null,HttpContext.Connection.RemoteIpAddress?.ToString()));
}
