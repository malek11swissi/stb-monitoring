namespace RneSimulator.Data;

public sealed class MongoSettings
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "mongodb://rne_user:rne_password@localhost:27017/?authSource=admin";
    public string DatabaseName { get; set; } = "rne_simulator";
    public string CompaniesCollection { get; set; } = "companies";
}
