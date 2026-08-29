using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using StbMonitoring.Application.Interfaces;

namespace StbMonitoring.Infrastructure.Notifications;

/// <summary>Canal e-mail SMTP Gmail. Aucun secret n'est conservé dans le dépôt.</summary>
public sealed class SmtpNotificationChannel(IConfiguration configuration) : INotificationChannel
{
    public object Status() => new
    {
        email = new
        {
            provider = "Gmail SMTP",
            mode = Mode,
            configured = Configured("Host") && Configured("Username") && Configured("Password") && Configured("SenderEmail")
        },
        sms = new { provider = "Disabled", mode = "Disabled", configured = false }
    };

    public async Task<ChannelSendResult> SendEmailAsync(string recipient,string subject,string htmlContent,CancellationToken ct)
    {
        if(Mode=="Simulation")return new(true,"SIMULATION","SENT",$"EMAIL-{Guid.NewGuid():N}",null);
        try
        {
            var host=Required("Host");var port=IntValue("Port",587);
            var username=Required("Username");var password=Required("Password");
            var senderEmail=Required("SenderEmail");var senderName=configuration["Notifications:Email:SenderName"]??"STB Monitoring";
            using var message=new MailMessage
            {
                From=new MailAddress(senderEmail,senderName),
                Subject=subject,
                Body=EmailTemplateBuilder.Wrap(subject,htmlContent),
                IsBodyHtml=true
            };
            message.To.Add(new MailAddress(recipient));
            using var smtp=new SmtpClient(host,port)
            {
                EnableSsl=BoolValue("UseTls",true),
                Credentials=new NetworkCredential(username,password.Replace(" ",string.Empty)),
                DeliveryMethod=SmtpDeliveryMethod.Network,
                UseDefaultCredentials=false,
                Timeout=IntValue("TimeoutMs",20000)
            };
            await smtp.SendMailAsync(message,ct);
            return new(true,"GMAIL_SMTP","SENT",null,null);
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            return new(false,"GMAIL_SMTP","FAILED",null,ex.Message);
        }
    }

    private string Mode => string.Equals(configuration["Notifications:Email:Mode"],"Smtp",StringComparison.OrdinalIgnoreCase)?"Smtp":"Simulation";
    private bool Configured(string key)=>!string.IsNullOrWhiteSpace(configuration[$"Notifications:Email:{key}"])&&!configuration[$"Notifications:Email:{key}"]!.StartsWith("COLLER_",StringComparison.OrdinalIgnoreCase);
    private string Required(string key)=>Configured(key)?configuration[$"Notifications:Email:{key}"]!:throw new InvalidOperationException($"Notifications:Email:{key} absent.");
    private int IntValue(string key,int fallback)=>int.TryParse(configuration[$"Notifications:Email:{key}"],out var value)?value:fallback;
    private bool BoolValue(string key,bool fallback)=>bool.TryParse(configuration[$"Notifications:Email:{key}"],out var value)?value:fallback;
}

internal static class EmailTemplateBuilder
{
    internal static string Wrap(string title,string content)=>$$"""
    <!doctype html><html lang="fr"><head><meta charset="utf-8"></head>
    <body style="margin:0;background:#f5f8fa;font-family:Arial,sans-serif;color:#18252e">
      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px;background:#f5f8fa"><tr><td align="center">
        <table role="presentation" width="620" cellspacing="0" cellpadding="0" style="max-width:620px;background:#fff;border:1px solid #dbe5ea;border-radius:14px;overflow:hidden">
          <tr><td style="padding:24px 30px;background:#123b5d;color:#fff"><div style="font-size:21px;font-weight:700">STB Monitoring</div><div style="margin-top:5px;color:#9edfe3;font-size:12px">Supervision opérationnelle sécurisée</div></td></tr>
          <tr><td style="padding:30px"><h1 style="font-size:22px;color:#123b5d;margin:0 0 20px">{{WebUtility.HtmlEncode(title)}}</h1><div style="font-size:15px;line-height:1.65">{{content}}</div></td></tr>
          <tr><td style="padding:18px 30px;background:#edf4f6;color:#61727e;font-size:12px">Message automatique de STB Monitoring · Ne communiquez jamais votre mot de passe.</td></tr>
        </table>
      </td></tr></table>
    </body></html>
    """;
}
