namespace StbMonitoring.Domain.Entities;
/// <summary>État persistant de l'escalade superviseur puis manager IT.</summary>
public sealed class IncidentEscalation
{
 private IncidentEscalation(){}public IncidentEscalation(Guid incidentId,int delayMinutes){Id=Guid.NewGuid();IncidentId=incidentId;NextEscalationAt=DateTime.UtcNow.AddMinutes(delayMinutes);}
 public Guid Id{get;private set;}public Guid IncidentId{get;private set;}public int Level{get;private set;}public DateTime NextEscalationAt{get;private set;}public DateTime? CompletedAt{get;private set;}public string? LastResult{get;private set;}
 public void Advance(string result,int delay){LastResult=result;Level++;if(Level>=2)CompletedAt=DateTime.UtcNow;else NextEscalationAt=DateTime.UtcNow.AddMinutes(delay);}
 public void Complete(string result){LastResult=result;CompletedAt=DateTime.UtcNow;}
}
