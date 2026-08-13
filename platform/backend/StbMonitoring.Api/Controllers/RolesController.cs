using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/roles"),Authorize(Roles=RoleNames.Admin)]
public sealed class RolesController(IIdentityStore store):ControllerBase
{
    [HttpGet] public async Task<IActionResult> All(CancellationToken ct)=>Ok((await store.GetRolesAsync(ct)).Select(x=>new RoleResponse(x.Id,x.Name,x.Description,x.RolePermissions.Select(p=>p.Permission.Name).ToArray())));
}
