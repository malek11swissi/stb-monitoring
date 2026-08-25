using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;
/// <summary>Proxy vers Nager.Date pour les jours fériés tunisiens du calendrier.</summary>
[ApiController,Route("api/holidays"),Authorize(Policy=PermissionNames.MaintenanceRead)]
public sealed class HolidaysController(IHttpClientFactory clients):ControllerBase
{[HttpGet("{year:int}")]public async Task<IActionResult>Get(int year,CancellationToken ct){if(year is <2020 or >2100)return BadRequest(new{message="Année invalide."});var client=clients.CreateClient();client.Timeout=TimeSpan.FromSeconds(8);using var response=await client.GetAsync($"https://date.nager.at/api/v3/PublicHolidays/{year}/TN",ct);if(!response.IsSuccessStatusCode)return StatusCode(503,new{message="Le service des jours fériés est temporairement indisponible."});return Content(await response.Content.ReadAsStringAsync(ct),"application/json");}}
