using StbMonitoring.Domain.Entities;
namespace StbMonitoring.Application.Interfaces;
public interface IIncidentNotificationDispatcher
{
    Task NotifyAssignmentAsync(Incident incident,CancellationToken ct);
}
