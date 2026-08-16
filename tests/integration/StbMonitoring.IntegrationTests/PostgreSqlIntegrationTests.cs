using Microsoft.EntityFrameworkCore;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;
using StbMonitoring.Application.Interfaces;
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

    [Fact]
    public async Task PostgreSql_persists_incident_comment_history_and_notification()
    {
        var options=new DbContextOptionsBuilder<MonitoringDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db=new MonitoringDbContext(options);await db.Database.MigrateAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var user=await db.Users.FirstAsync();var incident=new Incident($"INC-T-{Guid.NewGuid().ToString("N")[..12]}",null,null,null,"Incident intégration","Validation Sprint 3",IncidentCategory.Application,IncidentPriority.P3Medium,user.Id,null,120,480);
        db.Incidents.Add(incident);db.IncidentComments.Add(new IncidentComment(incident.Id,user.Id,"Analyse en cours","WorkNote",true));db.IncidentHistories.Add(new IncidentHistory(incident.Id,user.Id,"INCIDENT_CREATED"));db.Notifications.Add(new Notification(user.Id,"INCIDENT_CREATED",incident.IncidentNumber,incident.Title,AlertSeverity.Major,"Incident",incident.Id,$"/incidents/{incident.Id}"));await db.SaveChangesAsync();
        Assert.Single(await db.IncidentComments.Where(x=>x.IncidentId==incident.Id).ToListAsync());Assert.Single(await db.IncidentHistories.Where(x=>x.IncidentId==incident.Id).ToListAsync());Assert.Single(await db.Notifications.Where(x=>x.EntityId==incident.Id).ToListAsync());await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Incident_projection_can_be_loaded_by_operations_store()
    {
        var options=new DbContextOptionsBuilder<MonitoringDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db=new MonitoringDbContext(options);IOperationsStore store=new OperationsStore(db);
        var incidents=await store.GetIncidentsAsync(CancellationToken.None);
        Assert.NotNull(incidents);
    }

    [Fact]
    public async Task Incident_detail_can_be_loaded_by_id()
    {
        var options=new DbContextOptionsBuilder<MonitoringDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db=new MonitoringDbContext(options);IOperationsStore store=new OperationsStore(db);
        var existing=await db.Incidents.Select(x=>x.Id).FirstAsync();

        var detail=await store.GetIncidentDetailAsync(existing,CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(existing,detail.Incident.Id);
    }
}
