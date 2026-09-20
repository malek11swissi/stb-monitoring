using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using StbMonitoring.Application.Contracts;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Entities;

namespace StbMonitoring.Infrastructure.Monitoring;

/// <summary>
/// Un port ouvert ne prouve ni le rôle ni la réplication. MongoDB est interrogé
/// avec hello ; les autres moteurs restent explicitement en mode TCP seul.
/// </summary>
public sealed class DatabaseCheckExecutor(IConfiguration configuration) : ICheckExecutor
{
    public CheckType Type => CheckType.Database;

    public async Task<CheckExecution> ExecuteAsync(MonitoringEndpoint endpoint, CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        var watch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(endpoint.TimeoutSeconds));
        try
        {
            var host = endpoint.DatabaseHost ?? throw new InvalidOperationException("Hôte de base absent.");
            var port = endpoint.DatabasePort ?? throw new InvalidOperationException("Port de base absent.");
            string? observedRole = null;
            string replication = "non vérifiée";
            string verification = "TCP uniquement";
            long? replicationLagSeconds = null;
            string? replicationDetail = null;
            if (endpoint.DatabaseEngine == DatabaseEngine.MongoDb)
            {
                var settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(host, port),
                    DirectConnection = true,
                    ServerSelectionTimeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds),
                    ConnectTimeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds)
                };
                var user = configuration["Monitoring:Mongo:Username"];
                var password = configuration["Monitoring:Mongo:Password"];
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
                    settings.Credential = MongoCredential.CreateCredential("admin", user, password);
                var admin = new MongoClient(settings).GetDatabase("admin");
                var hello = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: timeout.Token);
                observedRole = hello.GetValue("isWritablePrimary", false).ToBoolean() ? "PRIMARY"
                    : hello.GetValue("secondary", false).ToBoolean() ? "SECONDARY" : "OTHER";
                verification = "MongoDB hello";
                if (hello.TryGetValue("setName", out var setName))
                    replication = string.Equals(setName.AsString, endpoint.DatabaseGroup, StringComparison.Ordinal) ? "membre du replica set" : "groupe différent";
                else replication = "aucun replica set détecté";
                if (replication == "membre du replica set")
                {
                    try
                    {
                        // Cette commande exige un compte MongoDB autorisé à lire l'état du replica set.
                        var state = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetStatus", 1), cancellationToken: timeout.Token);
                        var members = state["members"].AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                        var primary = members.FirstOrDefault(x => x.GetValue("stateStr", "").AsString == "PRIMARY");
                        var current = members.FirstOrDefault(x => x.GetValue("self", false).ToBoolean());
                        if (primary is not null && current is not null && primary.TryGetValue("optimeDate", out var primaryTime) && current.TryGetValue("optimeDate", out var currentTime)
                            && primaryTime.IsBsonDateTime && currentTime.IsBsonDateTime)
                        {
                            replicationLagSeconds = Math.Max(0, (long)(primaryTime.ToUniversalTime() - currentTime.ToUniversalTime()).TotalSeconds);
                            replicationDetail = "Écart des dernières opérations répliquées (optime), pas une garantie de zéro perte.";
                        }
                        else replicationDetail = "État du groupe disponible, écart d'optime non calculable.";
                    }
                    catch (MongoException) when (!timeout.IsCancellationRequested)
                    {
                        replicationDetail = "Écart non vérifié : droits MongoDB insuffisants ou état indisponible.";
                    }
                }
            }
            else
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port, timeout.Token);
            }
            watch.Stop();
            var mismatch = endpoint.DatabaseEngine == DatabaseEngine.MongoDb && replication != "membre du replica set";
            var status = mismatch ? MonitoringStatus.Degraded : watch.ElapsedMilliseconds >= endpoint.DownThresholdMs ? MonitoringStatus.Down
                : watch.ElapsedMilliseconds >= endpoint.DegradedThresholdMs ? MonitoringStatus.Degraded : MonitoringStatus.Up;
            var metadata = JsonSerializer.Serialize(new { engine=endpoint.DatabaseEngine?.ToString(), host, port, declaredRole=endpoint.DeclaredRole?.ToString(), observedRole, replication, verification, group=endpoint.DatabaseGroup, replicationLagSeconds, replicationDetail });
            return new(endpoint.Id,status,!mismatch,started,DateTime.UtcNow,watch.ElapsedMilliseconds,null,mismatch?"REPLICATION_GROUP":null,mismatch?"Le groupe de réplication déclaré ne correspond pas à l'instance.":null,metadata);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            watch.Stop();
            return new(endpoint.Id,MonitoringStatus.Down,false,started,DateTime.UtcNow,watch.ElapsedMilliseconds,null,"TIMEOUT","Délai de connexion à la base dépassé.",null);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            watch.Stop();
            // Ne pas persister une exception du driver : elle peut contenir
            // des informations internes de connexion ou d'authentification.
            return new(endpoint.Id,MonitoringStatus.Down,false,started,DateTime.UtcNow,watch.ElapsedMilliseconds,null,"DATABASE_CONNECTION","Connexion ou authentification de la base impossible.",null);
        }
    }
}
