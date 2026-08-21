using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Api.Security;
/// <summary>Émet le JWT signé contenant l'identité et le rôle fixe de l'utilisateur.</summary>
public sealed class JwtTokenService(IConfiguration config) : ITokenService
{
    public TokenResult Generate(AuthenticatedUser u)
    {
        var expires=DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpirationMinutes",60));
        var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,u.Id.ToString()),new(ClaimTypes.NameIdentifier,u.Id.ToString()),new(ClaimTypes.Name,u.Username),new(ClaimTypes.Email,u.Email)};
        claims.Add(new Claim(ClaimTypes.Role,u.Role));
        var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));var token=new JwtSecurityToken(config["Jwt:Issuer"],config["Jwt:Audience"],claims,expires:expires,signingCredentials:new(key,SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token),expires);
    }
}
