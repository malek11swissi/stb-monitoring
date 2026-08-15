using Microsoft.EntityFrameworkCore;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;
namespace StbMonitoring.IntegrationTests;
public sealed class PostgreSqlIntegrationTests
{
    private const string ConnectionString="Host=localhost;Port=55432;Database=stb_monitoring;Username=stb_admin;Password=ChangeMe_Postgres_123!";
    [Fact]
    public async Task PostgreSql_persists_and_enforces_unique_username()
    {
        var options=new DbContextOptionsBuilder<MonitoringDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db=new MonitoringDbContext(options);await db.Database.MigrateAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var username=$"integration_{Guid.NewGuid():N}";db.Users.Add(new User(username,$"{username}@test.local","hash","Integration","Test"));await db.SaveChangesAsync();
        db.Users.Add(new User(username,$"other_{username}@test.local","hash","Integration","Duplicate"));
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task PostgreSql_persists_monitoring_system_endpoint_and_result()
    {
        var options=new DbContextOptionsBuilder<MonitoringDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db=new MonitoringDbContext(options);await db.Database.MigrateAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var code=$"SYS_{Guid.NewGuid():N}";var system=new MonitoredSystem(code,"Integration System","Sprint 2",SystemEnvironment.Recette,SystemCriticality.High,"Tests");
        var endpoint=new MonitoringEndpoint(system.Id,"Health", "https://example.test/health",CheckType.Http,"GET",200,10,60,1000,3000,true,null,null);system.Endpoints.Add(endpoint);db.Systems.Add(system);
        db.CheckResults.Add(new CheckResult(system.Id,endpoint.Id,MonitoringStatus.Up,true,DateTime.UtcNow.AddMilliseconds(-10),DateTime.UtcNow,10,200,null,null,true,null,"integration"));await db.SaveChangesAsync();
        var persistedCode=system.Code;var loaded=await db.Systems.Include(x=>x.Endpoints).SingleAsync(x=>x.Code==persistedCode);Assert.Single(loaded.Endpoints);Assert.Equal(CheckType.Http,loaded.Endpoints.Single().CheckType);Assert.Single(await db.CheckResults.Where(x=>x.SystemId==system.Id).ToListAsync());await transaction.RollbackAsync();
    }
}
