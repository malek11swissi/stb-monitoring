using StbMonitoring.Domain.Entities;
namespace StbMonitoring.UnitTests;
public sealed class SprintThreeTests
{
 [Fact]public void Alert_counts_occurrences_and_can_be_acknowledged(){var user=Guid.NewGuid();var alert=new Alert("ALT-1",null,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),AlertEventType.EndpointDown,"API down","Timeout",AlertSeverity.Critical,"key","Timeout");alert.Occur(Guid.NewGuid(),"Still down");alert.Acknowledge(user);Assert.Equal(2,alert.OccurrenceCount);Assert.Equal(AlertStatus.Acknowledged,alert.Status);Assert.Equal(user,alert.AcknowledgedByUserId);}
 [Fact]public void Alert_requires_resolution_before_close(){var alert=new Alert("ALT-2",null,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),AlertEventType.EndpointDown,"API down","",AlertSeverity.Major,"key2",null);Assert.Throws<InvalidOperationException>(()=>alert.Close(Guid.NewGuid()));alert.Resolve();alert.Close(Guid.NewGuid());Assert.Equal(AlertStatus.Closed,alert.Status);}
 [Fact]public void Incident_follows_assignment_start_resolution_and_close(){var actor=Guid.NewGuid();var technician=Guid.NewGuid();var incident=new Incident("INC-1",null,null,null,"Panne Core","Description",IncidentCategory.Availability,IncidentPriority.P1Critical,actor,null,15,60);incident.Assign(technician,actor);incident.Start();incident.Resolve("Service redémarré","Processus arrêté");incident.Close(actor);Assert.Equal(IncidentStatus.Closed,incident.Status);Assert.NotNull(incident.FirstResponseAt);Assert.Equal("Service redémarré",incident.ResolutionSummary);}
 [Fact]public void Incident_can_be_reopened(){var actor=Guid.NewGuid();var incident=new Incident("INC-2",null,null,null,"Panne","Description",IncidentCategory.Application,IncidentPriority.P3Medium,actor,null,120,480);incident.Assign(actor,actor);incident.Start();incident.Resolve("Corrigé","Défaut applicatif");incident.Reopen();Assert.Equal(IncidentStatus.Reopened,incident.Status);Assert.Equal(1,incident.ReopenCount);}
 [Fact]public void Sla_becomes_breached_after_deadline(){var actor=Guid.NewGuid();var incident=new Incident("INC-3",null,null,null,"SLA","Description",IncidentCategory.Other,IncidentPriority.P1Critical,actor,null,1,1);incident.RefreshSla(DateTime.UtcNow.AddMinutes(2));Assert.Equal(SlaStatus.Breached,incident.SlaStatus);}
 [Fact]
 public void Incident_details_and_priority_can_be_updated()
 {
  var actor = Guid.NewGuid();
  var systemId = Guid.NewGuid();
  var incident = new Incident("INC-4", null, null, null, "Ancien titre", "Ancienne description", IncidentCategory.Other, IncidentPriority.P4Low, actor, null, 240, 1440);

  incident.UpdateDetails("Nouveau titre", "Description complete", IncidentCategory.Security, IncidentPriority.P1Critical, systemId, null, Guid.NewGuid(), 15, 60);

  Assert.Equal("Nouveau titre", incident.Title);
  Assert.Equal("Description complete", incident.Description);
  Assert.Equal(IncidentCategory.Security, incident.Category);
  Assert.Equal(IncidentPriority.P1Critical, incident.Priority);
  Assert.Equal(systemId, incident.SystemId);
  Assert.True(incident.ResolutionDueAt <= incident.CreatedAt.AddMinutes(60).AddSeconds(1));
 }
 [Fact]public void Notification_can_be_marked_read(){var n=new Notification(Guid.NewGuid(),"ALERT_CREATED","Alerte","Down",AlertSeverity.Critical,"Alert",Guid.NewGuid(),"/alerts/1");n.Read();Assert.True(n.IsRead);Assert.NotNull(n.ReadAt);}
 [Fact]public void Incident_rejects_invalid_lifecycle_transitions(){var actor=Guid.NewGuid();var incident=new Incident("INC-5",null,null,null,"Panne","Description",IncidentCategory.Application,IncidentPriority.P2High,actor,null,30,120);Assert.Throws<InvalidOperationException>(()=>incident.Pending());Assert.Throws<InvalidOperationException>(()=>incident.Resolve("Corrigé","Cause"));incident.Assign(actor,actor);incident.Start();Assert.Throws<ArgumentException>(()=>incident.Resolve("Corrigé",null));incident.Resolve("Corrigé","Cause confirmée");Assert.Throws<InvalidOperationException>(()=>incident.Assign(actor,actor));}
}
