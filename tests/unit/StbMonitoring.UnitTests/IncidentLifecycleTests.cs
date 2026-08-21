using StbMonitoring.Domain.Entities;
namespace StbMonitoring.UnitTests;
public sealed class IncidentLifecycleTests
{
 [Fact]public void DetectionToClosureLifecyclePreservesResolutionEvidence(){var technician=Guid.NewGuid();var supervisor=Guid.NewGuid();var incident=new Incident("INC-CYCLE-1",null,Guid.NewGuid(),Guid.NewGuid(),"Panne RNE","Mongo indisponible",IncidentCategory.Database,IncidentPriority.P1Critical,supervisor,null,15,60);incident.Assign(technician,supervisor);incident.Start();incident.Resolve("Mongo redémarré","Conteneur arrêté","Redémarrage et contrôle de connexion","Ajouter une alerte Docker","Capture du contrôle UP");incident.Close(supervisor);Assert.Equal(IncidentStatus.Closed,incident.Status);Assert.Equal("Conteneur arrêté",incident.RootCause);Assert.Equal("Redémarrage et contrôle de connexion",incident.CorrectiveAction);Assert.Equal("Ajouter une alerte Docker",incident.PreventiveAction);Assert.Equal("Capture du contrôle UP",incident.ResolutionEvidence);}
 [Fact]public void ResolutionRequiresCorrectiveAction(){var incident=new Incident("INC-CYCLE-2",null,null,null,"Test","Test",IncidentCategory.Other,IncidentPriority.P3Medium,Guid.NewGuid(),null,30,120);incident.Assign(Guid.NewGuid(),Guid.NewGuid());incident.Start();Assert.Throws<ArgumentException>(()=>incident.Resolve("Résolu",null,"",null,null));}
}
