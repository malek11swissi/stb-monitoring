using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StbMonitoring.Application.Interfaces;

namespace StbMonitoring.Infrastructure.Notifications;

/// <summary>
/// Adaptateur des fournisseurs externes : Brevo pour l'e-mail et Twilio pour
/// le SMS. Le mode Simulation permet de tester le cycle métier sans consommer
/// de quota ni exiger de secrets d'API.
/// </summary>
public sealed class ApiNotificationChannel(HttpClient client, IConfiguration configuration) : INotificationChannel
{
    public object Status() => new
    {
        email = new { provider = "Brevo", mode = Mode("Email"), configured = Configured("Email", "ApiKey") },
        sms = new { provider = "Twilio", mode = Mode("Sms"), configured = Configured("Sms", "AccountSid") && Configured("Sms", "AuthToken") && Configured("Sms", "FromNumber") }
    };

    public async Task<ChannelSendResult> SendEmailAsync(string recipient, string subject, string htmlContent, CancellationToken ct)
    {
        // En simulation, on renvoie un identifiant fictif avec le même contrat
        // qu'un fournisseur réel : le reste de l'application ne change pas.
        if (Mode("Email") == "Simulation") return new(true, "SIMULATION", "SENT", $"EMAIL-{Guid.NewGuid():N}", null);
        var apiKey = Required("Email", "ApiKey"); var senderEmail = Required("Email", "SenderEmail");
        var payload = JsonSerializer.Serialize(new { sender = new { name = configuration["Notifications:Email:SenderName"] ?? "STB Sentinel", email = senderEmail }, to = new[] { new { email = recipient } }, subject, htmlContent });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey); request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await SendAsync(request, "BREVO", ct);
    }

    public async Task<ChannelSendResult> SendSmsAsync(string recipient, string message, CancellationToken ct)
    {
        if (Mode("Sms") == "Simulation") return new(true, "SIMULATION", "QUEUED", $"SMS-{Guid.NewGuid():N}", null);
        var sid = Required("Sms", "AccountSid"); var token = Required("Sms", "AuthToken"); var from = Required("Sms", "FromNumber");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{token}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["To"] = recipient, ["From"] = from, ["Body"] = message });
        return await SendAsync(request, "TWILIO", ct);
    }

    private async Task<ChannelSendResult> SendAsync(HttpRequestMessage request, string provider, CancellationToken ct)
    {
        try
        {
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct); string? externalId = null; string? status = null;
            try { using var json = JsonDocument.Parse(body); if (json.RootElement.TryGetProperty("sid", out var sid)) externalId = sid.GetString(); else if (json.RootElement.TryGetProperty("messageId", out var id)) externalId = id.GetString(); if (json.RootElement.TryGetProperty("status", out var state)) status = state.GetString(); } catch (JsonException) { }
            return response.IsSuccessStatusCode ? new(true, provider, (status ?? "ACCEPTED").ToUpperInvariant(), externalId, null) : new(false, provider, "FAILED", externalId, $"HTTP {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return new(false, provider, "FAILED", null, ex.Message); }
    }

    private string Mode(string channel) => string.Equals(configuration[$"Notifications:{channel}:Mode"], "Api", StringComparison.OrdinalIgnoreCase) ? "Api" : "Simulation";
    private bool Configured(string channel, string name) => !string.IsNullOrWhiteSpace(configuration[$"Notifications:{channel}:{name}"]);
    private string Required(string channel, string name) => configuration[$"Notifications:{channel}:{name}"] ?? throw new InvalidOperationException($"Notifications:{channel}:{name} absent.");
}
