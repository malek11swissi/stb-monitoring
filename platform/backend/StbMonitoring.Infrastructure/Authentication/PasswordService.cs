using Microsoft.AspNetCore.Identity;
using StbMonitoring.Application.Interfaces;
namespace StbMonitoring.Infrastructure.Authentication;
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new(); private static readonly object Subject=new();
    public string Hash(string password)=>_hasher.HashPassword(Subject,password);
    public bool Verify(string hash,string password)=>_hasher.VerifyHashedPassword(Subject,hash,password)!=PasswordVerificationResult.Failed;
}
