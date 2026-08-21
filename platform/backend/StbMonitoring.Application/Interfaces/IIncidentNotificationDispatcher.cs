using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Application.Interfaces;
public interface IIncidentNotificationDispatcher{Task DispatchCriticalAsync(Incident incident,string stage,CancellationToken ct);}
