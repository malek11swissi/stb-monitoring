// API de diagnostic et test des fournisseurs Brevo et Twilio.
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

    [HttpPost("test-sms")]
    public async Task<IActionResult> TestSms(TestSmsRequest request, CancellationToken ct)
    {
        var result = await channels.SendSmsAsync(request.Recipient, request.Message, ct);
        return result.Success ? Ok(result) : StatusCode(502, result);
    }

    public sealed record TestEmailRequest(string Recipient, string Subject, string HtmlContent);
    public sealed record TestSmsRequest(string Recipient, string Message);
}
