using MongoDB.Bson.Serialization.Attributes;

namespace RneSimulator.Models;

public sealed class Company
{
    [BsonId] public string MatriculeFiscal { get; set; } = string.Empty;
    public string Denomination { get; set; } = string.Empty;
    public string Sigle { get; set; } = string.Empty;
    public string FormeJuridique { get; set; } = string.Empty;
    public string SituationJuridique { get; set; } = string.Empty;
    public string Gouvernorat { get; set; } = string.Empty;
    public DateTime DateImmatriculation { get; set; }
    public string CodeActivite { get; set; } = string.Empty;
}
