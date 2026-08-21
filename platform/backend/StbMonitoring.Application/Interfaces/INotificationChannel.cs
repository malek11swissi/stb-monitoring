namespace StbMonitoring.Application.Interfaces;

public sealed record ChannelSendResult(bool Success, string Provider, string Status, string? ExternalId, string? Error);

public interface INotificationChannel
{
    Task<ChannelSendResult> SendEmailAsync(string recipient, string subject, string htmlContent, CancellationToken ct);
    Task<ChannelSendResult> SendSmsAsync(string recipient, string message, CancellationToken ct);
    object Status();
}
