using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/users"),Authorize(Roles=RoleNames.Admin)]
public sealed class UsersController(IUserService users):ControllerBase
{
    [HttpGet] public async Task<IActionResult> All(CancellationToken ct)=>Ok(await users.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> One(Guid id,CancellationToken ct){var x=await users.GetByIdAsync(id,ct);return x is null?NotFound():Ok(x);}
    [HttpPost] public async Task<IActionResult> Create(CreateUserRequest r,CancellationToken ct){var x=await users.CreateAsync(r,Actor(),Ip(),ct);return CreatedAtAction(nameof(One),new{id=x.Id},x);}
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,UpdateUserRequest r,CancellationToken ct)=>Ok(await users.UpdateAsync(id,r,Actor(),Ip(),ct));
    [HttpPatch("{id:guid}/active")] public async Task<IActionResult> Active(Guid id,[FromQuery]bool value,CancellationToken ct){await users.SetActiveAsync(id,value,Actor(),Ip(),ct);return NoContent();}
    [HttpPost("{id:guid}/roles")] public async Task<IActionResult> Assign(Guid id,AssignRoleRequest r,CancellationToken ct){await users.AssignRoleAsync(id,r,Actor(),Ip(),ct);return NoContent();}
    [HttpDelete("{id:guid}/roles/{roleName}")] public async Task<IActionResult> Remove(Guid id,string roleName,CancellationToken ct){await users.RemoveRoleAsync(id,roleName,Actor(),Ip(),ct);return NoContent();}
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
}
