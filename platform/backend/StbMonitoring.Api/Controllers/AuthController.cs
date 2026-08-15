using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/auth")]
public sealed class AuthController(IAuthService auth,IUserService users):ControllerBase
{
    [HttpPost("login"),AllowAnonymous] public async Task<IActionResult> Login(LoginRequest r,CancellationToken ct){var result=await auth.LoginAsync(r,HttpContext.Connection.RemoteIpAddress?.ToString(),ct);return result is null?Unauthorized(new{message="Identifiants invalides ou compte désactivé."}):Ok(result);}
    [HttpGet("me"),Authorize] public async Task<IActionResult> Me(CancellationToken ct){var user=await users.GetByIdAsync(CurrentId(),ct);return user is null?NotFound():Ok(user);}
    [HttpPost("refresh"),Authorize] public async Task<IActionResult> Refresh(CancellationToken ct)=>Ok(await auth.RefreshAsync(CurrentId(),ct));
    [HttpPut("profile"),Authorize] public async Task<IActionResult> Profile(UpdateProfileRequest r,CancellationToken ct)=>Ok(await users.UpdateAsync(CurrentId(),new(r.Email,r.FirstName,r.LastName),CurrentId(),Ip(),ct));
    [HttpPost("change-password"),Authorize] public async Task<IActionResult> ChangePassword(ChangePasswordRequest r,CancellationToken ct){await auth.ChangePasswordAsync(CurrentId(),r,Ip(),ct);return NoContent();}
    private Guid CurrentId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
}
