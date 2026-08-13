using System.Text;
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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o=>{o.SwaggerDoc("v1",new(){Title="STB Monitoring API",Version="v1"});o.AddSecurityDefinition("Bearer",new(){Name="Authorization",Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT"});o.AddSecurityRequirement(new(){[new(){Reference=new(){Type=ReferenceType.SecurityScheme,Id="Bearer"}}]=Array.Empty<string>()});});
builder.Services.AddDbContext<MonitoringDbContext>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));
builder.Services.AddScoped<IIdentityStore,IdentityStore>(); builder.Services.AddScoped<IPasswordService,PasswordService>(); builder.Services.AddScoped<ITokenService,JwtTokenService>();
builder.Services.AddScoped<IAuthService,AuthService>(); builder.Services.AddScoped<IUserService,UserService>(); builder.Services.AddScoped<DatabaseSeeder>();
var jwt=builder.Configuration.GetSection("Jwt");var key=jwt["Key"]??throw new InvalidOperationException("Jwt:Key absent.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=jwt["Issuer"],ValidAudience=jwt["Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),ClockSkew=TimeSpan.FromMinutes(1)});
builder.Services.AddAuthorization();
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
