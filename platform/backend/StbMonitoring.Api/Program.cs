// Composition root : assemble toutes les couches, la sécurité et les workers.
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StbMonitoring.Api.Security;
using StbMonitoring.Api.Middleware;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Application.Services;
using StbMonitoring.Infrastructure.Authentication;
using StbMonitoring.Infrastructure.Persistence;
using StbMonitoring.Infrastructure.Monitoring;
using StbMonitoring.Api.Workers;
using StbMonitoring.Infrastructure.Notifications;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o=>{o.SwaggerDoc("v1",new(){Title="STB Monitoring API",Version="v1"});o.AddSecurityDefinition("Bearer",new(){Name="Authorization",Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT"});o.AddSecurityRequirement(new(){[new(){Reference=new(){Type=ReferenceType.SecurityScheme,Id="Bearer"}}]=Array.Empty<string>()});});
builder.Services.AddDbContext<MonitoringDbContext>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));
builder.Services.AddScoped<IIdentityStore,IdentityStore>(); builder.Services.AddScoped<IPasswordService,PasswordService>(); builder.Services.AddScoped<ITokenService,JwtTokenService>();
builder.Services.AddScoped<IAuthService,AuthService>(); builder.Services.AddScoped<IUserService,UserService>(); builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IMonitoringStore,MonitoringStore>();builder.Services.AddScoped<IMonitoringService,MonitoringService>();builder.Services.AddScoped<IOperationsStore,OperationsStore>();builder.Services.AddScoped<IOperationsService,OperationsService>();builder.Services.AddScoped<ICheckExecutor,HttpCheckExecutor>();builder.Services.AddScoped<ICheckExecutor,ApiJsonCheckExecutor>();builder.Services.AddScoped<ICheckExecutor,TlsCheckExecutor>();builder.Services.AddHostedService<MonitoringWorker>();builder.Services.AddHostedService<SlaWorker>();builder.Services.AddHostedService<AuditCleanupWorker>();
builder.Services.AddHttpClient<INotificationChannel,ApiNotificationChannel>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddScoped<IIncidentNotificationDispatcher,IncidentNotificationDispatcher>();
builder.Services.AddHostedService<IncidentEscalationWorker>();
var jwt=builder.Configuration.GetSection("Jwt");var key=jwt["Key"]??throw new InvalidOperationException("Jwt:Key absent.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=jwt["Issuer"],ValidAudience=jwt["Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),ClockSkew=TimeSpan.FromMinutes(1)});
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in StbMonitoring.Domain.Constants.PermissionNames.All)
        options.AddPolicy(permission, policy => policy.RequireRole(StbMonitoring.Domain.Constants.PermissionNames.RolesFor(permission)));
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (builder.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(origin =>
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                   && uri.IsLoopback
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        });
    }
    else
    {
        policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:4200");
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddExceptionHandler<ApiExceptionHandler>(); builder.Services.AddProblemDetails();
var app=builder.Build();
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("Frontend");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.MapGet("/health",()=>Results.Ok(new{status="UP",service="stb-monitoring-api"}));
using(var scope=app.Services.CreateScope()){await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();}
app.Run();
public partial class Program { }
