// API de diagnostic et test du canal SMTP Gmail.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;

namespace StbMonitoring.Api.Controllers;

[ApiController, Route("api/notification-channels"), Authorize(Policy = PermissionNames.NotificationsManage)]
public sealed class NotificationChannelsController(INotificationChannel channels) : ControllerBase
{
    [HttpGet("status")] public IActionResult Status() => Ok(channels.Status());

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(TestEmailRequest request, CancellationToken ct)
    {
        var result = await channels.SendEmailAsync(request.Recipient, request.Subject, request.HtmlContent, ct);
        return result.Success ? Ok(result) : StatusCode(502, result);
    }

    public sealed record TestEmailRequest(string Recipient, string Subject, string HtmlContent);
}
