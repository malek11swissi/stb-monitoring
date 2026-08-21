// API de consultation administrative des traces d'audit.
using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using StbMonitoring.Application.Contracts;using StbMonitoring.Application.Interfaces;using StbMonitoring.Domain.Constants;
namespace StbMonitoring.Api.Controllers;
[ApiController,Route("api/audit"),Authorize(Policy=PermissionNames.AuditRead)]
public sealed class AuditController(IIdentityStore store):ControllerBase{[HttpGet]public async Task<IActionResult> All(CancellationToken ct)=>Ok((await store.GetAuditLogsAsync(ct)).Select(x=>new AuditLogResponse(x.Id,x.UserId,x.Action,x.EntityName,x.EntityId,x.Details,x.IpAddress,x.CreatedAt,x.Success)));}
