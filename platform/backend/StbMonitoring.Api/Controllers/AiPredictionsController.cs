using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Application.Interfaces;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;

/// <summary>Expose les prédictions IA sans modifier les états métier UP/DOWN.</summary>
[ApiController,Route("api/ai"),Authorize(Roles=RoleNames.Admin+","+RoleNames.Supervisor+","+RoleNames.ManagerIt)]
public sealed class AiPredictionsController(IAiPredictionService predictions):ControllerBase
{
    [HttpGet("systems/{systemId:guid}/risk")]
    public async Task<IActionResult>SystemRisk(Guid systemId,CancellationToken ct)=>Ok(await predictions.PredictSystemRiskAsync(systemId,ct));
}
