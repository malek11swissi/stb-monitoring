using StbMonitoring.Application.Contracts;
namespace StbMonitoring.Application.Interfaces;
public interface IPasswordService { string Hash(string password); bool Verify(string passwordHash, string password); }
public interface ITokenService { TokenResult Generate(AuthenticatedUser user); }
