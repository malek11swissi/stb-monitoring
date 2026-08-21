namespace StbMonitoring.Domain.Entities;
/// <summary>Fichier lié à l'incident, éventuellement marqué comme preuve de résolution.</summary>
public sealed class IncidentAttachment
{
    private IncidentAttachment(){}
    public IncidentAttachment(Guid incidentId,Guid userId,string fileName,string contentType,byte[] content,bool proof){if(content.Length is 0 or >10485760)throw new ArgumentException("Fichier vide ou supérieur à 10 Mo.");Id=Guid.NewGuid();IncidentId=incidentId;UploadedByUserId=userId;FileName=Path.GetFileName(fileName);ContentType=contentType;Content=content;IsResolutionProof=proof;}
    public Guid Id{get;private set;}public Guid IncidentId{get;private set;}public Guid UploadedByUserId{get;private set;}public string FileName{get;private set;}="";public string ContentType{get;private set;}="application/octet-stream";public byte[] Content{get;private set;}=[];public bool IsResolutionProof{get;private set;}public DateTime CreatedAt{get;private set;}=DateTime.UtcNow;
}
