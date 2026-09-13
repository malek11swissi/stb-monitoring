using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Infrastructure.Monitoring;

/// <summary>Vérifie le code HTTP attendu et classe le temps de réponse.</summary>
public sealed class HttpCheckExecutor : ICheckExecutor { public CheckType Type => CheckType.Http; public Task<CheckExecution> ExecuteAsync(MonitoringEndpoint e, CancellationToken ct) => HttpRunner.RunAsync(e, false, ct); }
/// <summary>Ajoute au contrôle HTTP la validation d'une propriété du JSON retourné.</summary>
public sealed class ApiJsonCheckExecutor : ICheckExecutor { public CheckType Type => CheckType.ApiJson; public Task<CheckExecution> ExecuteAsync(MonitoringEndpoint e, CancellationToken ct) => HttpRunner.RunAsync(e, true, ct); }

internal static class HttpRunner
{
    public static async Task<CheckExecution> RunAsync(MonitoringEndpoint e, bool validateJson, CancellationToken ct)
    {
        var started = DateTime.UtcNow; var sw = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(e.TimeoutSeconds));
        try
        {
            using var client = new HttpClient(); using var request = new HttpRequestMessage(new HttpMethod(e.HttpMethod), e.Url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token); sw.Stop();
            var error = (int)response.StatusCode == e.ExpectedStatusCode ? null : $"Code HTTP {(int)response.StatusCode}, attendu {e.ExpectedStatusCode}.";
            if (error is null && validateJson && !string.IsNullOrWhiteSpace(e.ExpectedJsonProperty))
            {
                using var json = JsonDocument.Parse(body); JsonElement current = json.RootElement;
                foreach (var part in e.ExpectedJsonProperty.Split('.')) if (!current.TryGetProperty(part, out current)) { error = $"Propriété JSON {e.ExpectedJsonProperty} absente."; break; }
                if (error is null && e.ExpectedJsonValue is not null && !JsonValueMatches(current, e.ExpectedJsonValue)) error = $"Valeur JSON attendue {e.ExpectedJsonValue}, reçue {current}.";
            }
            // Une réponse correcte peut être DEGRADED ou DOWN si sa latence
            // dépasse les seuils configurés pour l'endpoint.
            var status = Calculate(e, sw.ElapsedMilliseconds, error);
            return new(e.Id, status, error is null, started, DateTime.UtcNow, sw.ElapsedMilliseconds, (int)response.StatusCode, error is null ? null : "VALIDATION", error, validateJson ? "API_JSON" : "HTTP");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { sw.Stop(); return new(e.Id, MonitoringStatus.Down, false, started, DateTime.UtcNow, sw.ElapsedMilliseconds, null, "TIMEOUT", "Délai d'attente dépassé.", null); }
        catch (Exception ex) { sw.Stop(); return new(e.Id, MonitoringStatus.Down, false, started, DateTime.UtcNow, sw.ElapsedMilliseconds, null, "NETWORK", ex.Message, null); }
    }

    private static bool JsonValueMatches(JsonElement value, string expected) => value.ValueKind switch { JsonValueKind.True => bool.TryParse(expected, out var parsed) && parsed, JsonValueKind.False => bool.TryParse(expected, out var parsed) && !parsed, JsonValueKind.String => string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase), _ => string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase) };
    private static MonitoringStatus Calculate(MonitoringEndpoint e, long ms, string? error) => error is not null || ms >= e.DownThresholdMs ? MonitoringStatus.Down : ms >= e.DegradedThresholdMs ? MonitoringStatus.Degraded : MonitoringStatus.Up;
}

/// <summary>
/// Ouvre directement une connexion TCP/TLS afin de vérifier le certificat
/// réellement présenté par le SI : dates, nom d'hôte, confiance et chaîne complète.
/// </summary>
public sealed class TlsCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Tls;

//verification tls de url -m
    public async Task<CheckExecution> ExecuteAsync(MonitoringEndpoint e, CancellationToken ct)
    {
        var started = DateTime.UtcNow; 
        var sw = Stopwatch.StartNew();
        try
        {
            var uri = new Uri(e.Url);
            if (uri.Scheme != Uri.UriSchemeHttps) return Failure(e, started, sw, "TLS_CONNECTION", "Le contrôle TLS exige une URL HTTPS.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(e.TimeoutSeconds));
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(uri.Host, uri.IsDefaultPort ? 443 : uri.Port, timeout.Token);

            SslPolicyErrors policyErrors = SslPolicyErrors.None;
            X509ChainStatus[] callbackStatuses = [];
            // Le callback accepte temporairement la négociation pour que nous puissions
            // capturer puis classer précisément l'erreur au lieu d'obtenir un échec générique.
            using var ssl = new SslStream(tcp.GetStream(), false, (_, _, chain, errors) => { policyErrors = errors; callbackStatuses = chain?.ChainStatus ?? []; return true; });
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = uri.Host, EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13, CertificateRevocationCheckMode = X509RevocationMode.NoCheck }, timeout.Token);

            if (ssl.RemoteCertificate is null) return Failure(e, started, sw, "TLS_CONNECTION", "Le serveur n'a présenté aucun certificat.");
            using var cert = new X509Certificate2(ssl.RemoteCertificate);
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            var chainTrusted = chain.Build(cert);
            var statuses = chain.ChainStatus.Concat(callbackStatuses).Select(x => x.Status.ToString()).Distinct().ToArray();
            var now = DateTime.UtcNow; var expiresAt = cert.NotAfter.ToUniversalTime(); var validFrom = cert.NotBefore.ToUniversalTime();
            var days = (int)Math.Floor((expiresAt - now).TotalDays);

            // L'ordre est volontaire : expiration et validité priment sur le nom,
            // la confiance puis l'avertissement d'expiration prochaine.
            string? errorType = null; string? message = null; var status = MonitoringStatus.Up;
            if (now > expiresAt || statuses.Contains(X509ChainStatusFlags.NotTimeValid.ToString())) { errorType = "TLS_EXPIRED"; message = $"Certificat expiré depuis le {expiresAt:O}."; status = MonitoringStatus.Down; }
            else if (now < validFrom || statuses.Contains(X509ChainStatusFlags.NotTimeNested.ToString())) { errorType = "TLS_NOT_YET_VALID"; message = $"Certificat non valide avant le {validFrom:O}."; status = MonitoringStatus.Down; }
            else if (policyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)) { errorType = "TLS_NAME_MISMATCH"; message = $"Le certificat ne correspond pas au nom d'hôte {uri.Host}."; status = MonitoringStatus.Down; }
            else if (!chainTrusted || policyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors)) { errorType = "TLS_UNTRUSTED"; message = "La chaîne du certificat n'est pas approuvée."; status = MonitoringStatus.Down; }
            else if (days <= 7) { errorType = "TLS_EXPIRING"; message = $"Le certificat expire dans {days} jours."; status = MonitoringStatus.Down; }
            else if (days <= 30) { errorType = "TLS_EXPIRING"; message = $"Le certificat expire bientôt, dans {days} jours."; status = MonitoringStatus.Degraded; }

            sw.Stop();
            var certificateChain = chain.ChainElements.Cast<X509ChainElement>().Select((element,index)=>new { index, subject=element.Certificate.Subject, issuer=element.Certificate.Issuer, thumbprint=element.Certificate.Thumbprint, serialNumber=element.Certificate.SerialNumber, validFrom=element.Certificate.NotBefore.ToUniversalTime(), expiresAt=element.Certificate.NotAfter.ToUniversalTime(), statuses=element.ChainElementStatus.Select(x=>x.Status.ToString()).ToArray() }).ToArray();
            var metadata = JsonSerializer.Serialize(new { host = uri.Host, subject = cert.Subject, issuer = cert.Issuer, thumbprint = cert.Thumbprint, serialNumber = cert.SerialNumber, validFrom, expiresAt, daysRemaining = days, protocol = ssl.SslProtocol.ToString(), chainTrusted, policyErrors = policyErrors.ToString(), chainStatuses = statuses, certificateChain });
            return new(e.Id, status, status == MonitoringStatus.Up, started, DateTime.UtcNow, sw.ElapsedMilliseconds, null, errorType, message, metadata);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Failure(e, started, sw, "TLS_CONNECTION", "Délai de connexion TLS dépassé."); }
        catch (SocketException ex) { return Failure(e, started, sw, "TLS_CONNECTION", $"Connexion TCP/TLS impossible : {ex.Message}"); }
        catch (AuthenticationException ex) { return Failure(e, started, sw, "TLS_HANDSHAKE", $"Négociation TLS impossible : {ex.Message}"); }
        catch (Exception ex) { return Failure(e, started, sw, "TLS_CONNECTION", ex.Message); }
    }

    private static CheckExecution Failure(MonitoringEndpoint e, DateTime started, Stopwatch sw, string type, string message)
    {
        sw.Stop(); return new(e.Id, MonitoringStatus.Down, false, started, DateTime.UtcNow, sw.ElapsedMilliseconds, null, type, message, null);
    }
}
