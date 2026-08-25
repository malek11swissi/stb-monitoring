// Frontière HTTP de l'identité : login, renouvellement, profil et mot de passe.
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
    [HttpPut("profile"),Authorize] public async Task<IActionResult> Profile(UpdateProfileRequest r,CancellationToken ct)=>Ok(await users.UpdateProfileAsync(CurrentId(),r,Ip(),ct));
    [HttpPost("avatar"),Authorize,RequestSizeLimit(2_200_000)]
    public async Task<IActionResult> Avatar(IFormFile file,CancellationToken ct)
    {
        if(file.Length is 0 or > 2_097_152)return BadRequest(new{message="La photo doit avoir une taille maximale de 2 Mo."});
        var formats=new Dictionary<string,(string Extension,byte[][] Signatures)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"]=(".jpg",[[0xFF,0xD8,0xFF]]),
            ["image/png"]=(".png",[[0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A]]),
            ["image/webp"]=(".webp",[[0x52,0x49,0x46,0x46]])
        };
        if(!formats.TryGetValue(file.ContentType,out var format))return BadRequest(new{message="Formats autorisés : JPG, PNG ou WebP."});
        var header=new byte[12];await using(var input=file.OpenReadStream()){var read=await input.ReadAsync(header,ct);if(read<4||!format.Signatures.Any(s=>header.AsSpan().StartsWith(s)))return BadRequest(new{message="Le contenu du fichier ne correspond pas à une image valide."});}
        var relative=Path.Combine("uploads","avatars",$"{CurrentId():N}{format.Extension}");
        var full=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot",relative);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using(var output=System.IO.File.Create(full))await file.CopyToAsync(output,ct);
        return Ok(await users.UpdateAvatarAsync(CurrentId(),relative,CurrentId(),Ip(),ct));
    }
    [HttpPost("change-password"),Authorize] public async Task<IActionResult> ChangePassword(ChangePasswordRequest r,CancellationToken ct){await auth.ChangePasswordAsync(CurrentId(),r,Ip(),ct);return NoContent();}
    private Guid CurrentId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
}
