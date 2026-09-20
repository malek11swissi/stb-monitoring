using StbMonitoring.Domain.Entities;

namespace StbMonitoring.UnitTests;

public sealed class DatabaseEndpointTests
{
    [Fact]
    public void Database_endpoint_keeps_host_port_and_declared_role()
    {
        var endpoint = Create("rne-mongo-2", 27017, "rs0");

        Assert.Equal(CheckType.Database, endpoint.CheckType);
        Assert.Equal("mongodb://rne-mongo-2:27017", endpoint.Url);
        Assert.Equal(DatabaseEngine.MongoDb, endpoint.DatabaseEngine);
        Assert.Equal(DatabaseRole.Secondary, endpoint.DeclaredRole);
        Assert.Equal("rs0", endpoint.DatabaseGroup);
    }

    [Theory]
    [InlineData("rne-mongo-2", 0, "rs0")]
    [InlineData("rne-mongo-2", 65536, "rs0")]
    [InlineData("http://rne-mongo-2", 27017, "rs0")]
    [InlineData("rne-mongo-2", 27017, "")]
    public void Database_endpoint_rejects_invalid_address_or_group(string host, int port, string group)
    {
        Assert.Throws<ArgumentException>(() => Create(host, port, group));
    }

    [Fact]
    public void Available_secondary_keeps_system_degraded_not_down()
    {
        var system = new MonitoredSystem("rne", "RNE", "", SystemEnvironment.Recette, SystemCriticality.High, null);
        var primary = new MonitoringEndpoint(system.Id, "RNE primaire", null, CheckType.Database, "GET", 200, 5, 30, 1000, 5000, true, null, null, DatabaseEngine.MongoDb, "rne-mongo-1", 27017, null, "rs0", DatabaseRole.Primary);
        var secondary = Create("rne-mongo-2", 27017, "rs0");
        system.Endpoints.Add(primary);
        system.Endpoints.Add(secondary);
        primary.Record(MonitoringStatus.Down, 5000, "Panne");
        secondary.Record(MonitoringStatus.Up, 50, null);

        system.RecalculateStatus();

        Assert.Equal(MonitoringStatus.Degraded, system.Status);
    }

    private static MonitoringEndpoint Create(string host, int port, string group) =>
        new(Guid.NewGuid(), "RNE secondaire", null, CheckType.Database, "GET", 200,
            5, 30, 1000, 5000, false, null, null, DatabaseEngine.MongoDb,
            host, port, "rne_simulator", group, DatabaseRole.Secondary);
}
