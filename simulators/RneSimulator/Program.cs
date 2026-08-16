using System.Text.Json.Serialization;
using RneSimulator.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<ScenarioState>();
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
builder.Services.AddSingleton<CompanyRepository>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var state = context.RequestServices.GetRequiredService<ScenarioState>();
    state.ClearIfExpired();
    if (!context.Request.Path.StartsWithSegments("/api/v1/entreprises")) { await next(); return; }
    if (state.Current is Scenario.HighLatency or Scenario.Timeout) await Task.Delay(state.LatencyMs, context.RequestAborted);
    if (state.Current == Scenario.Http500) { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { code = "RNE-500", message = "Erreur interne simulée du registre national des entreprises." }); return; }
    if (state.Current == Scenario.Unavailable) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "RNE-503", message = "Le service de consultation RNE est temporairement indisponible." }); return; }
    if (state.Current == Scenario.DatabaseDown) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "RNE-DB-001", message = "La source de données du registre est indisponible.", dependency = "MongoDB", status = "DOWN" }); return; }
    await next();
});

app.MapGet("/api/v1/entreprises/{matriculeFiscal}", async (string matriculeFiscal, ScenarioState state, CompanyRepository repository, CancellationToken ct) =>
{
    if (state.Current == Scenario.InvalidResponse) return Results.Ok(new { matricule = matriculeFiscal, denomination = (string?)null, situationJuridique = (string?)null });
    if (state.Current == Scenario.EmptyResponse) return Results.NoContent();
    try { var company = await repository.FindAsync(matriculeFiscal, ct); return company is null ? Results.NotFound(new { code = "RNE-404", message = $"Aucune entreprise trouvée pour le matricule fiscal {matriculeFiscal}." }) : Results.Ok(company); }
    catch (Exception) when (!ct.IsCancellationRequested) { return MongoUnavailable(); }
}).WithName("ConsulterEntreprise");

app.MapGet("/api/v1/entreprises/{matriculeFiscal}/situation-juridique", async (string matriculeFiscal, CompanyRepository repository, CancellationToken ct) =>
{
    try { var company = await repository.FindAsync(matriculeFiscal, ct); return company is null ? Results.NotFound(new { code = "RNE-404", message = "Entreprise introuvable." }) : Results.Ok(new { company.MatriculeFiscal, company.Denomination, situation = company.SituationJuridique, registreAccessible = true, source = "MONGODB", checkedAt = DateTime.UtcNow }); }
    catch (Exception) when (!ct.IsCancellationRequested) { return MongoUnavailable(); }
});

app.MapGet("/api/v1/entreprises", async (string? denomination, string? gouvernorat, CompanyRepository repository, CancellationToken ct) =>
{
    try { return Results.Ok(await repository.SearchAsync(denomination, gouvernorat, ct)); }
    catch (Exception) when (!ct.IsCancellationRequested) { return MongoUnavailable(); }
});

app.MapGet("/api/v1/registre/disponibilite", async (ScenarioState state, CompanyRepository repository, CancellationToken ct) =>
{
    var databaseUp = state.Current != Scenario.DatabaseDown && await repository.IsAvailableAsync(ct);
    var available = state.Current is not (Scenario.Unavailable or Scenario.Http500) && databaseUp;
    return Results.Json(new { service = "Registre National des Entreprises", code = "RNE-TN", disponible = available, sourceDonnees = new { type = "MongoDB", database = "rne_simulator", collection = "companies", disponible = databaseUp }, scenario = state.Current, message = available ? "Le registre et MongoDB sont disponibles." : "Connexion MongoDB impossible ou scénario de panne actif.", checkedAt = DateTime.UtcNow }, statusCode: available ? 200 : 503);
});

app.MapGet("/api/lab/scenario", (ScenarioState state) => Results.Ok(state.View()));
app.MapPost("/api/lab/scenario", (ScenarioRequest request, HttpRequest http, IConfiguration config, ScenarioState state) =>
{
    if (http.Headers["X-Lab-Key"] != config["Lab:AdminKey"]) return Results.Forbid();
    if (request.LatencyMs is < 0 or > 120000 || request.DurationMinutes is < 1 or > 120) return Results.BadRequest(new { message = "Latence ou durée de scénario invalide." });
    state.Activate(request); return Results.Ok(state.View());
});
app.MapDelete("/api/lab/scenario", (HttpRequest http, IConfiguration config, ScenarioState state) =>
{
    if (http.Headers["X-Lab-Key"] != config["Lab:AdminKey"]) return Results.Forbid();
    state.Reset(); return Results.Ok(state.View());
});

try
{
    var repository = app.Services.GetRequiredService<CompanyRepository>();
    if (await repository.IsAvailableAsync(CancellationToken.None)) await repository.SeedAsync(CancellationToken.None);
    else app.Logger.LogWarning("MongoDB indisponible au démarrage. Le service démarre en mode dégradé et retournera HTTP 503.");
}
catch (Exception ex) { app.Logger.LogWarning(ex, "Initialisation MongoDB impossible. Le service reste disponible pour le diagnostic."); }

app.Run();

static IResult MongoUnavailable() => Results.Json(new { code = "RNE-DB-001", message = "Connexion MongoDB impossible.", dependency = "MongoDB", status = "DOWN" }, statusCode: 503);
record ScenarioRequest(Scenario Scenario, int LatencyMs = 0, int DurationMinutes = 15, string? Reason = null);
enum Scenario { Normal, Http500, Unavailable, HighLatency, Timeout, DatabaseDown, InvalidResponse, EmptyResponse }
sealed class ScenarioState
{
    public Scenario Current { get; private set; } = Scenario.Normal; public int LatencyMs { get; private set; } public DateTime? ActivatedAt { get; private set; } public DateTime? ExpiresAt { get; private set; } public string? Reason { get; private set; }
    public void Activate(ScenarioRequest x) { Current = x.Scenario; LatencyMs = x.Scenario == Scenario.Timeout ? Math.Max(30000, x.LatencyMs) : x.LatencyMs; ActivatedAt = DateTime.UtcNow; ExpiresAt = DateTime.UtcNow.AddMinutes(x.DurationMinutes); Reason = x.Reason; }
    public void Reset() { Current = Scenario.Normal; LatencyMs = 0; ActivatedAt = null; ExpiresAt = null; Reason = null; }
    public void ClearIfExpired() { if (ExpiresAt <= DateTime.UtcNow) Reset(); }
    public object View() { ClearIfExpired(); return new { scenario = Current, latencyMs = LatencyMs, activatedAt = ActivatedAt, expiresAt = ExpiresAt, reason = Reason }; }
}
public partial class Program { }
