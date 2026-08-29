using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RneSimulator.Models;

namespace RneSimulator.Data;

public sealed class CompanyRepository
{
    private readonly IMongoDatabase database;
    private readonly IMongoCollection<Company> companies;

    public CompanyRepository(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
        var client = new MongoClient(clientSettings);
        database = client.GetDatabase(settings.DatabaseName);
        companies = database.GetCollection<Company>(settings.CompaniesCollection);
    }


    // verifier database down ou up 
    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try { await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct); return true; }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }

        //Recherche une entreprise par matricule fiscal
    public async Task<Company?> FindAsync(string matricule, CancellationToken ct) => 
    
        await companies.Find(x => x.MatriculeFiscal == matricule.ToUpperInvariant()).FirstOrDefaultAsync(ct);


    //Recherche plusieurs entreprises selon denomination et  gouvernorat

    public async Task<IReadOnlyCollection<Company>> SearchAsync(string? denomination, string? gouvernorat, CancellationToken ct)
    {
        var filter = Builders<Company>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(denomination)) filter &= Builders<Company>.Filter.Regex(x => x.Denomination, new BsonRegularExpression(denomination, "i"));
        if (!string.IsNullOrWhiteSpace(gouvernorat)) filter &= Builders<Company>.Filter.Eq(x => x.Gouvernorat, gouvernorat.ToUpperInvariant());
        return await companies.Find(filter).SortBy(x => x.Denomination).ToListAsync(ct);
    }

     //add 3 companies 
    public async Task SeedAsync(CancellationToken ct)
    { // >0 : Cela évite de recréer les mêmes entreprises à chaque redémarrage. 
        if (await companies.EstimatedDocumentCountAsync(cancellationToken: ct) > 0) return;
        await companies.InsertManyAsync(new[]
        {
            New("0000012A", "SOCIETE TUNISIENNE DE BANQUE", "STB", "SOCIETE ANONYME", "ACTIVE", "TUNIS", 1958, 1, 18, "6419"),
            New("1234567B", "TUNISIE SERVICES NUMERIQUES", "TSN", "SOCIETE A RESPONSABILITE LIMITEE", "ACTIVE", "ARIANA", 2018, 4, 12, "6201"),
            New("7654321C", "INDUSTRIES DU CAP BON", "ICB", "SOCIETE ANONYME", "SUSPENDED", "NABEUL", 2005, 9, 3, "2229")
        }, cancellationToken: ct);
    }

    private static Company New(string id, string name, 
    string sigle, string forme, string situation, string gouvernorat, 
    int year, int month, int day, string activity) => new() { MatriculeFiscal = id, Denomination = name, Sigle = sigle, FormeJuridique = forme, SituationJuridique = situation, Gouvernorat = gouvernorat, DateImmatriculation = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), CodeActivite = activity };
}
