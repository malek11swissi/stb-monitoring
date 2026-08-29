// Frontière HTTP de l'identité : login, renouvellement, profil et mot de passe.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/auth")]
public sealed class AuthController(IAuthService auth,IUserService users,INotificationChannel notifications,IConfiguration configuration):ControllerBase
{
    [HttpPost("login"),AllowAnonymous] public async Task<IActionResult> Login(LoginRequest r,CancellationToken ct){var result=await auth.LoginAsync(r,HttpContext.Connection.RemoteIpAddress?.ToString(),ct);return result is null?Unauthorized(new{message="Identifiants invalides ou compte désactivé."}):Ok(result);}
    [HttpPost("verify-2fa"),AllowAnonymous] public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest r,CancellationToken ct){var result=await auth.VerifyTwoFactorAsync(r,Ip(),ct);return result is null?Unauthorized(new{message="Code invalide, expiré ou nombre maximal d’essais atteint."}):Ok(result);}
    [HttpGet("me"),Authorize] public async Task<IActionResult> Me(CancellationToken ct){var user=await users.GetByIdAsync(CurrentId(),ct);return user is null?NotFound():Ok(user);}
    [HttpPost("refresh"),Authorize] public async Task<IActionResult> Refresh(CancellationToken ct)=>Ok(await auth.RefreshAsync(CurrentId(),ct));
    [HttpPost("forgot-password"),AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request,CancellationToken ct)
    {
        var token=await auth.ForgotPasswordAsync(request,Ip(),ct);
        if(token is not null)
        {
            var frontend=(configuration["FrontendUrl"]??"http://localhost:4200").TrimEnd('/');
            var link=$"{frontend}/reset-password?token={Uri.EscapeDataString(token)}";
            await notifications.SendEmailAsync(request.Email,"Réinitialisation de votre mot de passe STB Monitoring",$"<h2>Réinitialisation du mot de passe</h2><p>Une demande de récupération a été reçue.</p><p><a href=\"{link}\">Choisir un nouveau mot de passe</a></p><p>Ce lien est personnel et expire dans 30 minutes. Si vous n’êtes pas à l’origine de la demande, ignorez cet e-mail.</p>",ct);
        }
        return Accepted(new{message="Si cette adresse correspond à un compte actif, un e-mail de récupération a été envoyé."});
    }
    [HttpPost("reset-password"),AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request,CancellationToken ct){await auth.ResetPasswordAsync(request,Ip(),ct);return NoContent();}
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
        foreach(var old in Directory.EnumerateFiles(Path.GetDirectoryName(full)!, $"{CurrentId():N}.*").Where(path=>!string.Equals(path,full,StringComparison.OrdinalIgnoreCase)))System.IO.File.Delete(old);
        return Ok(await users.UpdateAvatarAsync(CurrentId(),relative,CurrentId(),Ip(),ct));
    }
    [HttpDelete("avatar"),Authorize]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var directory=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","avatars");
        if(Directory.Exists(directory))foreach(var path in Directory.EnumerateFiles(directory,$"{CurrentId():N}.*"))System.IO.File.Delete(path);
        return Ok(await users.UpdateAvatarAsync(CurrentId(),null,CurrentId(),Ip(),ct));
    }
    [HttpPost("change-password"),Authorize] public async Task<IActionResult> ChangePassword(ChangePasswordRequest r,CancellationToken ct){await auth.ChangePasswordAsync(CurrentId(),r,Ip(),ct);return NoContent();}
    private Guid CurrentId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
}
