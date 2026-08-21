// API des KPI et exports CSV, PDF et Excel mensuels ou annuels.
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StbMonitoring.Domain.Constants;
using StbMonitoring.Domain.Entities;
using StbMonitoring.Infrastructure.Persistence;
using StbMonitoring.Api.Reporting;

namespace StbMonitoring.Api.Controllers;

[ApiController, Route("api/reporting"), Authorize(Policy = PermissionNames.ReportingRead)]
public sealed class ReportingController(MonitoringDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? systemId, CancellationToken ct)
    {
        var end = (to ?? DateTime.UtcNow).ToUniversalTime();
        var start = (from ?? end.AddDays(-30)).ToUniversalTime();
        if (start > end) return BadRequest(new { message = "La date de début doit précéder la date de fin." });

        var technician = User.IsInRole(RoleNames.Technician);
        var userId = Actor();
        var incidentsQuery = db.Incidents.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt <= end);
        if (technician) incidentsQuery = incidentsQuery.Where(x => x.AssignedToUserId == userId);
        if (systemId.HasValue) incidentsQuery = incidentsQuery.Where(x => x.SystemId == systemId);
        var incidents = await incidentsQuery.ToArrayAsync(ct);

        var checksQuery = db.CheckResults.AsNoTracking().Where(x => x.StartedAt >= start && x.StartedAt <= end);
        if (systemId.HasValue) checksQuery = checksQuery.Where(x => x.SystemId == systemId);
        var checks = technician ? [] : await checksQuery.ToArrayAsync(ct);
        var alerts = technician ? [] : await db.Alerts.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt <= end && (!systemId.HasValue || x.SystemId == systemId)).ToArrayAsync(ct);
        var systems = await db.Systems.AsNoTracking().Where(x => !x.IsArchived).ToArrayAsync(ct);

        var resolved = incidents.Where(x => x.ResolvedAt.HasValue).ToArray();
        var closedStates = new[] { IncidentStatus.Resolved, IncidentStatus.Closed, IncidentStatus.Cancelled };
        var open = incidents.Count(x => !closedStates.Contains(x.Status));
        var availability = checks.Length == 0 ? 0 : Math.Round(checks.Count(x => x.Status == MonitoringStatus.Up) * 100d / checks.Length, 1);
        var meanResolution = resolved.Length == 0 ? 0 : Math.Round(resolved.Average(x => (x.ResolvedAt!.Value - x.CreatedAt).TotalMinutes), 1);
        var meanResponse = incidents.Where(x => x.FirstResponseAt.HasValue).Select(x => (x.FirstResponseAt!.Value - x.CreatedAt).TotalMinutes).DefaultIfEmpty(0).Average();
        var checksById = checks.ToDictionary(x => x.Id);
        var mttd = alerts.Where(x => checksById.ContainsKey(x.FirstCheckResultId)).Select(x => (x.FirstDetectedAt - checksById[x.FirstCheckResultId].StartedAt).TotalMinutes).DefaultIfEmpty(0).Average();
        var mtta = alerts.Where(x => x.AcknowledgedAt.HasValue).Select(x => (x.AcknowledgedAt!.Value - x.FirstDetectedAt).TotalMinutes).DefaultIfEmpty(0).Average();
        var slaMet = incidents.Count(x => x.SlaStatus == SlaStatus.Met);
        var slaMeasured = incidents.Count(x => x.SlaStatus is SlaStatus.Met or SlaStatus.Breached);
        var systemHealth = technician ? [] : systems.Select(s => new SystemHealth(s.Id, s.Name, s.Status.ToString(), checks.Count(x => x.SystemId == s.Id), checks.Any(x => x.SystemId == s.Id) ? Math.Round(checks.Count(x => x.SystemId == s.Id && x.Status == MonitoringStatus.Up) * 100d / checks.Count(x => x.SystemId == s.Id), 1) : 0, Math.Round(checks.Where(x => x.SystemId == s.Id).Select(x => (double)x.DurationMs).DefaultIfEmpty(0).Average(), 0))).ToArray();

        var trends = Enumerable.Range(0, Math.Min(30, Math.Max(1, (end.Date - start.Date).Days + 1))).Select(offset => start.Date.AddDays(offset)).Select(day => new
        {
            date = day,
            incidents = incidents.Count(x => x.CreatedAt.Date == day),
            alerts = technician ? 0 : alerts.Count(x => x.CreatedAt.Date == day)
        });

        return Ok(new
        {
            from = start, to = end, generatedAt = DateTime.UtcNow, personal = technician,
            kpis = new { systems = technician ? 0 : systems.Length, availability, openAlerts = technician ? 0 : alerts.Count(x => x.Status is AlertStatus.Open or AlertStatus.Acknowledged), openIncidents = open, unassignedIncidents = technician ? 0 : incidents.Count(x => x.AssignedToUserId == null && !closedStates.Contains(x.Status)), slaBreached = incidents.Count(x => x.SlaStatus == SlaStatus.Breached), slaAtRisk = incidents.Count(x => x.SlaStatus == SlaStatus.AtRisk), meanDetectionMinutes = technician ? 0 : Math.Round(mttd, 1), meanAcknowledgementMinutes = technician ? 0 : Math.Round(mtta, 1), meanResolutionMinutes = meanResolution, meanResponseMinutes = Math.Round(meanResponse, 1), slaComplianceRate = slaMeasured == 0 ? 100 : Math.Round(slaMet * 100d / slaMeasured, 1) },
            incidentsByStatus = CountBy(incidents, x => x.Status.ToString()),
            incidentsByPriority = CountBy(incidents, x => x.Priority.ToString()),
            incidentsByCategory = CountBy(incidents, x => x.Category.ToString()),
            slaByStatus = CountBy(incidents, x => x.SlaStatus.ToString()),
            alertsBySeverity = technician ? Array.Empty<Breakdown>() : CountBy(alerts, x => x.Severity.ToString()),
            systemHealth,
            trends
        });
    }

    [HttpGet("incidents.csv"), Authorize(Policy = PermissionNames.ReportingExport)]
    public async Task<IActionResult> ExportIncidents([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var end = (to ?? DateTime.UtcNow).ToUniversalTime(); var start = (from ?? end.AddDays(-30)).ToUniversalTime();
        var query = from incident in db.Incidents.AsNoTracking() join user in db.Users.AsNoTracking() on incident.AssignedToUserId equals user.Id into users from user in users.DefaultIfEmpty() where incident.CreatedAt >= start && incident.CreatedAt <= end select new { incident, technician = user == null ? "Non affecté" : user.FirstName + " " + user.LastName };
        if (User.IsInRole(RoleNames.Technician)) { var actor = Actor(); query = query.Where(x => x.incident.AssignedToUserId == actor); }
        var rows = await query.OrderByDescending(x => x.incident.CreatedAt).ToArrayAsync(ct);
        var csv = new StringBuilder("Numéro;Titre;Priorité;Statut;Technicien;SLA;Création;Résolution;Résumé\r\n");
        foreach (var row in rows) csv.AppendLine(string.Join(';', Csv(row.incident.IncidentNumber), Csv(row.incident.Title), row.incident.Priority, row.incident.Status, Csv(row.technician), row.incident.SlaStatus, row.incident.CreatedAt.ToString("O", CultureInfo.InvariantCulture), row.incident.ResolvedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "", Csv(row.incident.ResolutionSummary)));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", $"rapport-incidents-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet("incidents.pdf"), Authorize(Policy = PermissionNames.ReportingExport)]
    public async Task<IActionResult> ExportPdf([FromQuery]string period="custom",[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {var(start,end)=Period(period,from,to);var rows=await ReportRows(start,end,ct);return File(ReportFileBuilder.Pdf($"STB Sentinel — Rapport {PeriodLabel(period)} du {start:dd/MM/yyyy} au {end:dd/MM/yyyy}",rows),"application/pdf",$"rapport-{period}-{end:yyyyMMdd}.pdf");}

    [HttpGet("incidents.xlsx"), Authorize(Policy = PermissionNames.ReportingExport)]
    public async Task<IActionResult> ExportExcel([FromQuery]string period="custom",[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {var(start,end)=Period(period,from,to);var rows=await ReportRows(start,end,ct);return File(ReportFileBuilder.Xlsx(rows),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"rapport-{period}-{end:yyyyMMdd}.xlsx");}

    private async Task<IReadOnlyList<string[]>> ReportRows(DateTime start,DateTime end,CancellationToken ct)
    {var q=db.Incidents.AsNoTracking().Where(x=>x.CreatedAt>=start&&x.CreatedAt<=end);if(User.IsInRole(RoleNames.Technician)){var actor=Actor();q=q.Where(x=>x.AssignedToUserId==actor);}return await q.OrderByDescending(x=>x.CreatedAt).Select(x=>new[]{x.IncidentNumber,x.Title,x.Priority.ToString(),x.Status.ToString(),x.CreatedAt.ToString("dd/MM/yyyy HH:mm"),x.ResolvedAt.HasValue?x.ResolvedAt.Value.ToString("dd/MM/yyyy HH:mm"):""}).ToArrayAsync(ct);}
    private static(DateTime Start,DateTime End)Period(string period,DateTime? from,DateTime? to){var end=(to??DateTime.UtcNow).ToUniversalTime();return period.ToLowerInvariant() switch{"monthly"=>(new DateTime(end.Year,end.Month,1,0,0,0,DateTimeKind.Utc),end),"annual"=>(new DateTime(end.Year,1,1,0,0,0,DateTimeKind.Utc),end),_=>(from?.ToUniversalTime()??end.AddDays(-30),end)};}
    private static string PeriodLabel(string period)=>period.ToLowerInvariant() switch{"monthly"=>"mensuel","annual"=>"annuel",_=>"personnalisé"};

    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static Breakdown[] CountBy<T>(IEnumerable<T> values, Func<T, string> key) => values.GroupBy(key).Select(x => new Breakdown(x.Key, x.Count())).OrderByDescending(x => x.Count).ToArray();
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private sealed record Breakdown(string Key, int Count);
    private sealed record SystemHealth(Guid Id, string Name, string Status, int Checks, double Availability, double AverageResponseMs);
}
