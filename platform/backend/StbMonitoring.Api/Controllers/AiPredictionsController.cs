using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;

/// <summary>Expose les prédictions IA sans modifier les états métier UP/DOWN.</summary>
[ApiController,Route("api/ai"),Authorize]
public sealed class AiPredictionsController(IAiPredictionService predictions):ControllerBase
{
    [HttpGet("systems/{systemId:guid}/risk"),Authorize(Roles=RoleNames.Supervisor+","+RoleNames.ManagerIt)]
    public async Task<IActionResult>SystemRisk(Guid systemId,CancellationToken ct)=>Ok(await predictions.PredictSystemRiskAsync(systemId,ct));

    [HttpGet("incidents/{incidentId:guid}/resolution-recommendations"),Authorize(Roles=RoleNames.Technician)]
    public async Task<IActionResult>ResolutionRecommendations(Guid incidentId,CancellationToken ct)=>Ok(await predictions.RecommendResolutionAsync(incidentId,UserId(),ct));

    [HttpGet("incidents/{incidentId:guid}/technician-recommendations"),Authorize(Roles=RoleNames.Supervisor)]
    public async Task<IActionResult>TechnicianRecommendations(Guid incidentId,CancellationToken ct)=>Ok(await predictions.RecommendTechnicianAsync(incidentId,ct));

    private Guid UserId()=>Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??throw new UnauthorizedAccessException());
}
