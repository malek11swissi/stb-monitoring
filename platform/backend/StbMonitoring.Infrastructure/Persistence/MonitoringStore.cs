using Microsoft.EntityFrameworkCore;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Infrastructure.Persistence;
public sealed class MonitoringStore(MonitoringDbContext db):IMonitoringStore
{
 private IQueryable<MonitoredSystem> Systems=>db.Systems.Include(x=>x.Endpoints);
 public async Task<IReadOnlyCollection<MonitoredSystem>>GetSystemsAsync(bool archived,CancellationToken ct)=>await Systems.Where(x=>archived||!x.IsArchived).OrderBy(x=>x.Name).ToArrayAsync(ct);
 public Task<MonitoredSystem?>GetSystemAsync(Guid id,CancellationToken ct)=>Systems.SingleOrDefaultAsync(x=>x.Id==id,ct);
 public Task<bool>SystemCodeExistsAsync(string code,Guid? excluded,CancellationToken ct)=>db.Systems.AnyAsync(x=>(!excluded.HasValue||x.Id!=excluded)&&x.Code==code.Trim().ToUpper(),ct);
 public Task<MonitoringEndpoint?>GetEndpointAsync(Guid id,CancellationToken ct)=>db.MonitoringEndpoints.Include(x=>x.System).SingleOrDefaultAsync(x=>x.Id==id,ct);
 public async Task<IReadOnlyCollection<MonitoringEndpoint>>GetDueEndpointsAsync(DateTime now,CancellationToken ct)=>await db.MonitoringEndpoints.Where(x=>x.IsActive&&x.System.MonitoringEnabled&&!x.System.IsArchived&&x.NextCheckAt<=now).OrderBy(x=>x.NextCheckAt).Take(100).ToArrayAsync(ct);
 public async Task<IReadOnlyCollection<CheckResult>>GetResultsAsync(Guid? systemId,Guid? endpointId,MonitoringStatus? status,DateTime? from,DateTime? to,int take,CancellationToken ct)=>await db.CheckResults.Include(x=>x.Endpoint).Where(x=>(!systemId.HasValue||x.SystemId==systemId)&&(!endpointId.HasValue||x.EndpointId==endpointId)&&(!status.HasValue||x.Status==status)&&(!from.HasValue||x.StartedAt>=from)&&(!to.HasValue||x.StartedAt<=to)).OrderByDescending(x=>x.StartedAt).Take(take).ToArrayAsync(ct);
 public Task<bool>SystemHasResultsAsync(Guid systemId,CancellationToken ct)=>db.CheckResults.AnyAsync(x=>x.SystemId==systemId,ct);
 public Task<bool>EndpointHasResultsAsync(Guid endpointId,CancellationToken ct)=>db.CheckResults.AnyAsync(x=>x.EndpointId==endpointId,ct);
 public void AddSystem(MonitoredSystem x)=>db.Systems.Add(x);public void RemoveSystem(MonitoredSystem x)=>db.Systems.Remove(x);public void AddEndpoint(MonitoringEndpoint x)=>db.MonitoringEndpoints.Add(x);public void RemoveEndpoint(MonitoringEndpoint x)=>db.MonitoringEndpoints.Remove(x);public void AddResult(CheckResult x)=>db.CheckResults.Add(x);public async Task SaveChangesAsync(CancellationToken ct)=>await db.SaveChangesAsync(ct);
}
