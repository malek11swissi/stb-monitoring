using System.Text.Json.Serialization;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<LabState>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var state = context.RequestServices.GetRequiredService<LabState>(); state.ClearIfExpired();
    if (!context.Request.Path.StartsWithSegments("/api/v1/messages") && !context.Request.Path.StartsWithSegments("/api/v1/campagnes")) { await next(); return; }
    if (state.Current is LabScenario.HighLatency or LabScenario.Timeout) await Task.Delay(state.LatencyMs, context.RequestAborted);
    if (state.Current == LabScenario.Http500) { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { code = "SMS-500", message = "Erreur interne simulée de la passerelle SMS." }); return; }
    if (state.Current == LabScenario.Unavailable) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "SMS-503", message = "La passerelle SMS est indisponible." }); return; }
    if (state.Current == LabScenario.DatabaseDown) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "SMS-DB-001", message = "La base MySQL SMS est indisponible.", dependency = "MySQL", status = "DOWN" }); return; }
    await next();
});

app.MapGet("/api/v1/messages/{reference}", async (string reference, IConfiguration config, LabState state, CancellationToken ct) =>
{
    if (state.Current == LabScenario.InvalidResponse) return Results.Ok(new { reference, statut = (string?)null });
    try { await using var db = await OpenAsync(config, ct); await using var cmd = new MySqlCommand("SELECT reference, destinataire, contenu, statut, created_at FROM messages WHERE reference=@reference", db); cmd.Parameters.AddWithValue("@reference", reference.ToUpperInvariant()); await using var row = await cmd.ExecuteReaderAsync(ct); return await row.ReadAsync(ct) ? Results.Ok(new { reference = row.GetString(0), destinataire = row.GetString(1), contenu = row.GetString(2), statut = row.GetString(3), createdAt = row.GetDateTime(4) }) : Results.NotFound(new { code = "SMS-404", message = "Message introuvable." }); }
    catch (Exception) when (!ct.IsCancellationRequested) { return DbUnavailable(); }
});
app.MapGet("/api/v1/messages/{reference}/statut-livraison", async (string reference, IConfiguration config, CancellationToken ct) =>
{
    try { await using var db = await OpenAsync(config, ct); await using var cmd = new MySqlCommand("SELECT statut FROM messages WHERE reference=@reference", db); cmd.Parameters.AddWithValue("@reference", reference.ToUpperInvariant()); var status = await cmd.ExecuteScalarAsync(ct); return status is null ? Results.NotFound(new { code = "SMS-404", message = "Message introuvable." }) : Results.Ok(new { reference = reference.ToUpperInvariant(), statutLivraison = status.ToString(), serviceSmsAccessible = true, source = "MYSQL", checkedAt = DateTime.UtcNow }); }
    catch (Exception) when (!ct.IsCancellationRequested) { return DbUnavailable(); }
});
app.MapGet("/api/v1/campagnes/{code}", async (string code, IConfiguration config, CancellationToken ct) =>
{
    try { await using var db = await OpenAsync(config, ct); await using var cmd = new MySqlCommand("SELECT code, libelle, statut FROM campagnes WHERE code=@code", db); cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant()); await using var row = await cmd.ExecuteReaderAsync(ct); return await row.ReadAsync(ct) ? Results.Ok(new { code = row.GetString(0), libelle = row.GetString(1), statut = row.GetString(2) }) : Results.NotFound(new { code = "SMS-CAMP-404", message = "Campagne introuvable." }); }
    catch (Exception) when (!ct.IsCancellationRequested) { return DbUnavailable(); }
});

MapLab(app);
await InitializeAsync(app);
app.Run();

static async Task<MySqlConnection> OpenAsync(IConfiguration config, CancellationToken ct) { var db = new MySqlConnection(config.GetConnectionString("MySql")); await db.OpenAsync(ct); return db; }
static async Task InitializeAsync(WebApplication app)
{
    try { await using var db = await OpenAsync(app.Configuration, CancellationToken.None); const string sql = """
        CREATE TABLE IF NOT EXISTS messages (reference VARCHAR(40) PRIMARY KEY, destinataire VARCHAR(20) NOT NULL, contenu VARCHAR(255) NOT NULL, statut VARCHAR(30) NOT NULL, created_at DATETIME NOT NULL);
        CREATE TABLE IF NOT EXISTS campagnes (code VARCHAR(40) PRIMARY KEY, libelle VARCHAR(150) NOT NULL, statut VARCHAR(30) NOT NULL);
        INSERT IGNORE INTO messages VALUES ('SMS-STB-001','21620111222','Code de confirmation STB','DELIVERED',UTC_TIMESTAMP());
        INSERT IGNORE INTO campagnes VALUES ('CAMP-ALERTE-STB','Alertes opérationnelles STB','ACTIVE');
        """; await using var cmd = new MySqlCommand(sql, db); await cmd.ExecuteNonQueryAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "MySQL indisponible au démarrage. Les API métier retourneront HTTP 503."); }
}
static IResult DbUnavailable() => Results.Json(new { code = "SMS-DB-001", message = "Connexion MySQL impossible.", dependency = "MySQL", status = "DOWN" }, statusCode: 503);
static void MapLab(WebApplication app) { app.MapGet("/api/lab/scenario", (LabState s) => Results.Ok(s.View())); app.MapPost("/api/lab/scenario", (LabRequest x, HttpRequest h, IConfiguration c, LabState s) => { if (h.Headers["X-Lab-Key"] != c["Lab:AdminKey"]) return Results.Forbid(); if (x.LatencyMs is < 0 or > 120000 || x.DurationMinutes is < 1 or > 120) return Results.BadRequest(); s.Activate(x); return Results.Ok(s.View()); }); app.MapDelete("/api/lab/scenario", (HttpRequest h, IConfiguration c, LabState s) => { if (h.Headers["X-Lab-Key"] != c["Lab:AdminKey"]) return Results.Forbid(); s.Reset(); return Results.Ok(s.View()); }); }
record LabRequest(LabScenario Scenario, int LatencyMs = 0, int DurationMinutes = 15, string? Reason = null);
enum LabScenario { Normal, Http500, Unavailable, HighLatency, Timeout, DatabaseDown, InvalidResponse }
sealed class LabState { public LabScenario Current { get; private set; } public int LatencyMs { get; private set; } public DateTime? ExpiresAt { get; private set; } public string? Reason { get; private set; } public void Activate(LabRequest x) { Current=x.Scenario; LatencyMs=x.Scenario==LabScenario.Timeout?Math.Max(30000,x.LatencyMs):x.LatencyMs; ExpiresAt=DateTime.UtcNow.AddMinutes(x.DurationMinutes); Reason=x.Reason; } public void Reset(){Current=LabScenario.Normal;LatencyMs=0;ExpiresAt=null;Reason=null;} public void ClearIfExpired(){if(ExpiresAt<=DateTime.UtcNow)Reset();} public object View(){ClearIfExpired();return new{scenario=Current,latencyMs=LatencyMs,expiresAt=ExpiresAt,reason=Reason};} }
public partial class Program { }
