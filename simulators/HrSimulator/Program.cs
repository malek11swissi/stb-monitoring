using System.Text.Json.Serialization;
using HrSimulator.Data;
using HrSimulator.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<HrDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sql => sql.EnableRetryOnFailure(2, TimeSpan.FromSeconds(1), null)));
builder.Services.AddSingleton<LabState>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var state = context.RequestServices.GetRequiredService<LabState>(); state.ClearIfExpired();
    if (!context.Request.Path.StartsWithSegments("/api/v1/employes") && !context.Request.Path.StartsWithSegments("/api/v1/departements")) { await next(); return; }
    if (state.Current is LabScenario.HighLatency or LabScenario.Timeout) await Task.Delay(state.LatencyMs, context.RequestAborted);
    if (state.Current == LabScenario.Http500) { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { code = "RH-500", message = "Erreur interne simulée du système RH." }); return; }
    if (state.Current == LabScenario.Unavailable) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "RH-503", message = "Le service RH est temporairement indisponible." }); return; }
    if (state.Current == LabScenario.DatabaseDown) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new { code = "RH-DB-001", message = "La base SQL Server RH est indisponible.", dependency = "SQL Server", status = "DOWN" }); return; }
    await next();
});

app.MapGet("/api/v1/employes/{matricule}", async (string matricule, HrDbContext db, LabState state, CancellationToken ct) =>
{
    if (state.Current == LabScenario.InvalidResponse) return Results.Ok(new { matricule, nom = (string?)null, statut = (string?)null });
    try { var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Matricule == matricule.ToUpper(), ct); return employee is null ? Results.NotFound(new { code = "RH-404", message = "Employé introuvable." }) : Results.Ok(employee); }
    catch (Exception) when (!ct.IsCancellationRequested) { return SqlUnavailable(); }
});

app.MapGet("/api/v1/employes/{matricule}/situation-professionnelle", async (string matricule, HrDbContext db, CancellationToken ct) =>
{
    try { var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Matricule == matricule.ToUpper(), ct); return employee is null ? Results.NotFound(new { code = "RH-404", message = "Employé introuvable." }) : Results.Ok(new { employee.Matricule, nomComplet = employee.FirstName + " " + employee.LastName, employee.JobTitle, employee.DepartmentCode, statut = employee.EmploymentStatus, donneesRhAccessibles = true, source = "SQLSERVER", checkedAt = DateTime.UtcNow }); }
    catch (Exception) when (!ct.IsCancellationRequested) { return SqlUnavailable(); }
});

app.MapGet("/api/v1/employes", async (string? nom, string? departement, HrDbContext db, CancellationToken ct) =>
{
    try { var query = db.Employees.AsNoTracking(); if (!string.IsNullOrWhiteSpace(nom)) query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(nom)); if (!string.IsNullOrWhiteSpace(departement)) query = query.Where(x => x.DepartmentCode == departement.ToUpper()); return Results.Ok(await query.OrderBy(x => x.LastName).ToArrayAsync(ct)); }
    catch (Exception) when (!ct.IsCancellationRequested) { return SqlUnavailable(); }
});

app.MapGet("/api/v1/departements/{code}", async (string code, HrDbContext db, CancellationToken ct) =>
{
    try { var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code.ToUpper(), ct); return department is null ? Results.NotFound(new { code = "RH-DEP-404", message = "Département introuvable." }) : Results.Ok(department); }
    catch (Exception) when (!ct.IsCancellationRequested) { return SqlUnavailable(); }
});

app.MapGet("/api/lab/scenario", (LabState state) => Results.Ok(state.View()));
app.MapPost("/api/lab/scenario", (LabRequest request, HttpRequest http, IConfiguration config, LabState state) => { if (http.Headers["X-Lab-Key"] != config["Lab:AdminKey"]) return Results.Forbid(); if (request.LatencyMs is < 0 or > 120000 || request.DurationMinutes is < 1 or > 120) return Results.BadRequest(new { message = "Paramètres invalides." }); state.Activate(request); return Results.Ok(state.View()); });
app.MapDelete("/api/lab/scenario", (HttpRequest http, IConfiguration config, LabState state) => { if (http.Headers["X-Lab-Key"] != config["Lab:AdminKey"]) return Results.Forbid(); state.Reset(); return Results.Ok(state.View()); });

await InitializeAsync(app);
app.Run();

static async Task InitializeAsync(WebApplication app)
{
    try { using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<HrDbContext>(); await db.Database.EnsureCreatedAsync(); if (await db.Employees.AnyAsync()) return; db.Departments.AddRange(new Department { Code = "DSI", Name = "Direction des Systèmes d'Information", Location = "Tunis" }, new Department { Code = "DRH", Name = "Direction des Ressources Humaines", Location = "Tunis" }); db.Employees.AddRange(new Employee { Matricule = "STB-TECH-001", FirstName = "Amine", LastName = "Trabelsi", DepartmentCode = "DSI", JobTitle = "Ingénieur systèmes", EmploymentStatus = "ACTIVE", HiredAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) }, new Employee { Matricule = "STB-RH-001", FirstName = "Sarra", LastName = "Ben Salem", DepartmentCode = "DRH", JobTitle = "Responsable RH", EmploymentStatus = "ACTIVE", HiredAt = new DateTime(2019, 9, 2, 0, 0, 0, DateTimeKind.Utc) }); await db.SaveChangesAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "SQL Server indisponible au démarrage. Les endpoints métier retourneront HTTP 503."); }
}
static IResult SqlUnavailable() => Results.Json(new { code = "RH-DB-001", message = "Connexion SQL Server impossible.", dependency = "SQL Server", status = "DOWN" }, statusCode: 503);
record LabRequest(LabScenario Scenario, int LatencyMs = 0, int DurationMinutes = 15, string? Reason = null);
enum LabScenario { Normal, Http500, Unavailable, HighLatency, Timeout, DatabaseDown, InvalidResponse }
sealed class LabState { public LabScenario Current { get; private set; } = LabScenario.Normal; public int LatencyMs { get; private set; } public DateTime? ActivatedAt { get; private set; } public DateTime? ExpiresAt { get; private set; } public string? Reason { get; private set; } public void Activate(LabRequest x) { Current = x.Scenario; LatencyMs = x.Scenario == LabScenario.Timeout ? Math.Max(30000, x.LatencyMs) : x.LatencyMs; ActivatedAt = DateTime.UtcNow; ExpiresAt = DateTime.UtcNow.AddMinutes(x.DurationMinutes); Reason = x.Reason; } public void Reset() { Current = LabScenario.Normal; LatencyMs = 0; ActivatedAt = null; ExpiresAt = null; Reason = null; } public void ClearIfExpired() { if (ExpiresAt <= DateTime.UtcNow) Reset(); } public object View() { ClearIfExpired(); return new { scenario = Current, latencyMs = LatencyMs, activatedAt = ActivatedAt, expiresAt = ExpiresAt, reason = Reason }; } }
public partial class Program { }
