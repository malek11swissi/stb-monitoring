using System.Text.Json.Serialization;
using Oracle.ManagedDataAccess.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<LabState>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var state = context.RequestServices.GetRequiredService<LabState>(); state.ClearIfExpired();
    if (!context.Request.Path.StartsWithSegments("/api/v1/comptes") && !context.Request.Path.StartsWithSegments("/api/v1/agences")) { await next(); return; }
    if (state.Current is LabScenario.HighLatency or LabScenario.Timeout) await Task.Delay(state.LatencyMs, context.RequestAborted);
    if (state.Current == LabScenario.Http500) { context.Response.StatusCode=500; await context.Response.WriteAsJsonAsync(new { code="CORE-500", message="Erreur interne simulée du Core Banking." }); return; }
    if (state.Current == LabScenario.Unavailable) { context.Response.StatusCode=503; await context.Response.WriteAsJsonAsync(new { code="CORE-503", message="Le service Core Banking est indisponible." }); return; }
    if (state.Current == LabScenario.DatabaseDown) { context.Response.StatusCode=503; await context.Response.WriteAsJsonAsync(new { code="CORE-DB-001", message="La base Oracle est indisponible.", dependency="Oracle", status="DOWN" }); return; }
    await next();
});

app.MapGet("/api/v1/comptes/{numero}", async (string numero, IConfiguration config, LabState state, CancellationToken ct) =>
{
    if (state.Current == LabScenario.InvalidResponse) return Results.Ok(new { numero, statut=(string?)null, solde=(decimal?)null });
    try { await using var db=await OpenAsync(config,ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT NUMERO, INTITULE, SOLDE, DEVISE, STATUT, CODE_AGENCE FROM COMPTES WHERE NUMERO=:numero"; cmd.Parameters.Add(new OracleParameter("numero",numero)); await using var row=await cmd.ExecuteReaderAsync(ct); return await row.ReadAsync(ct) ? Results.Ok(new { numero=row.GetString(0), intitule=row.GetString(1), solde=row.GetDecimal(2), devise=row.GetString(3), statut=row.GetString(4), codeAgence=row.GetString(5) }) : Results.NotFound(new { code="CORE-404", message="Compte introuvable." }); }
    catch(Exception) when(!ct.IsCancellationRequested){return DbUnavailable();}
});
app.MapGet("/api/v1/comptes/{numero}/position", async (string numero, IConfiguration config, CancellationToken ct) =>
{
    try { await using var db=await OpenAsync(config,ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT SOLDE, DEVISE, STATUT FROM COMPTES WHERE NUMERO=:numero"; cmd.Parameters.Add(new OracleParameter("numero",numero)); await using var row=await cmd.ExecuteReaderAsync(ct); return await row.ReadAsync(ct) ? Results.Ok(new { numero, soldeDisponible=row.GetDecimal(0), devise=row.GetString(1), statutCompte=row.GetString(2), coreBankingAccessible=true, source="ORACLE", checkedAt=DateTime.UtcNow }) : Results.NotFound(new { code="CORE-404", message="Compte introuvable." }); }
    catch(Exception) when(!ct.IsCancellationRequested){return DbUnavailable();}
});
app.MapGet("/api/v1/agences/{code}", async (string code, IConfiguration config, CancellationToken ct) =>
{
    try { await using var db=await OpenAsync(config,ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT CODE, LIBELLE, VILLE FROM AGENCES WHERE CODE=:code"; cmd.Parameters.Add(new OracleParameter("code",code.ToUpperInvariant())); await using var row=await cmd.ExecuteReaderAsync(ct); return await row.ReadAsync(ct) ? Results.Ok(new { code=row.GetString(0), libelle=row.GetString(1), ville=row.GetString(2) }) : Results.NotFound(new { code="CORE-AG-404", message="Agence introuvable." }); }
    catch(Exception) when(!ct.IsCancellationRequested){return DbUnavailable();}
});

MapLab(app);
await InitializeAsync(app);
app.Run();

static async Task<OracleConnection> OpenAsync(IConfiguration config,CancellationToken ct){var db=new OracleConnection(config.GetConnectionString("Oracle"));await db.OpenAsync(ct);return db;}
static async Task InitializeAsync(WebApplication app)
{
    try { await using var db=await OpenAsync(app.Configuration,CancellationToken.None); var statements=new[]{
        "BEGIN EXECUTE IMMEDIATE 'CREATE TABLE AGENCES (CODE VARCHAR2(20) PRIMARY KEY, LIBELLE VARCHAR2(150) NOT NULL, VILLE VARCHAR2(80) NOT NULL)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
        "BEGIN EXECUTE IMMEDIATE 'CREATE TABLE COMPTES (NUMERO VARCHAR2(34) PRIMARY KEY, INTITULE VARCHAR2(150) NOT NULL, SOLDE NUMBER(18,3) NOT NULL, DEVISE VARCHAR2(3) NOT NULL, STATUT VARCHAR2(30) NOT NULL, CODE_AGENCE VARCHAR2(20) NOT NULL)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
        "MERGE INTO AGENCES a USING (SELECT 'AG-TUN-001' CODE, 'Agence Tunis Centre' LIBELLE, 'Tunis' VILLE FROM dual) s ON (a.CODE=s.CODE) WHEN NOT MATCHED THEN INSERT (CODE,LIBELLE,VILLE) VALUES(s.CODE,s.LIBELLE,s.VILLE)",
        "MERGE INTO COMPTES a USING (SELECT 'TN5901000000000012345678' NUMERO, 'Compte démonstration STB' INTITULE, 12540.750 SOLDE, 'TND' DEVISE, 'OUVERT' STATUT, 'AG-TUN-001' CODE_AGENCE FROM dual) s ON (a.NUMERO=s.NUMERO) WHEN NOT MATCHED THEN INSERT (NUMERO,INTITULE,SOLDE,DEVISE,STATUT,CODE_AGENCE) VALUES(s.NUMERO,s.INTITULE,s.SOLDE,s.DEVISE,s.STATUT,s.CODE_AGENCE)"}; foreach(var sql in statements){await using var cmd=db.CreateCommand();cmd.CommandText=sql;await cmd.ExecuteNonQueryAsync();} }
    catch(Exception ex){app.Logger.LogWarning(ex,"Oracle indisponible au démarrage. Les API métier retourneront HTTP 503.");}
}
static IResult DbUnavailable()=>Results.Json(new{code="CORE-DB-001",message="Connexion Oracle impossible.",dependency="Oracle",status="DOWN"},statusCode:503);
static void MapLab(WebApplication app){app.MapGet("/api/lab/scenario",(LabState s)=>Results.Ok(s.View()));app.MapPost("/api/lab/scenario",(LabRequest x,HttpRequest h,IConfiguration c,LabState s)=>{if(h.Headers["X-Lab-Key"]!=c["Lab:AdminKey"])return Results.Forbid();if(x.LatencyMs is <0 or >120000||x.DurationMinutes is <1 or >120)return Results.BadRequest();s.Activate(x);return Results.Ok(s.View());});app.MapDelete("/api/lab/scenario",(HttpRequest h,IConfiguration c,LabState s)=>{if(h.Headers["X-Lab-Key"]!=c["Lab:AdminKey"])return Results.Forbid();s.Reset();return Results.Ok(s.View());});}
record LabRequest(LabScenario Scenario,int LatencyMs=0,int DurationMinutes=15,string? Reason=null);
enum LabScenario{Normal,Http500,Unavailable,HighLatency,Timeout,DatabaseDown,InvalidResponse}
sealed class LabState{public LabScenario Current{get;private set;}public int LatencyMs{get;private set;}public DateTime? ExpiresAt{get;private set;}public string? Reason{get;private set;}public void Activate(LabRequest x){Current=x.Scenario;LatencyMs=x.Scenario==LabScenario.Timeout?Math.Max(30000,x.LatencyMs):x.LatencyMs;ExpiresAt=DateTime.UtcNow.AddMinutes(x.DurationMinutes);Reason=x.Reason;}public void Reset(){Current=LabScenario.Normal;LatencyMs=0;ExpiresAt=null;Reason=null;}public void ClearIfExpired(){if(ExpiresAt<=DateTime.UtcNow)Reset();}public object View(){ClearIfExpired();return new{scenario=Current,latencyMs=LatencyMs,expiresAt=ExpiresAt,reason=Reason};}}
public partial class Program { }
