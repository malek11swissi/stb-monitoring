using Microsoft.AspNetCore.Diagnostics;
namespace StbMonitoring.Api.Middleware;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger):IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception ex,CancellationToken ct){logger.LogWarning(ex,"Erreur API contrôlée");context.Response.StatusCode=ex switch{KeyNotFoundException=>404,ArgumentException=>400,InvalidOperationException=>409,_=>500};await context.Response.WriteAsJsonAsync(new{message=ex.Message},ct);return true;}
}
