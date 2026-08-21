using StbMonitoring.Domain.Entities;

namespace StbMonitoring.UnitTests;

public sealed class MaintenanceWindowTests
{
    [Fact]
    public void Maintenance_requires_a_target_and_valid_period()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => new MaintenanceWindow("Test", null, null, null, now, now.AddHours(1), true, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new MaintenanceWindow("Test", null, Guid.NewGuid(), null, now, now, true, Guid.NewGuid()));
    }

    [Fact]
    public void Maintenance_can_be_cancelled()
    {
        var now = DateTime.UtcNow;
        var maintenance = new MaintenanceWindow("Oracle", "Mise à jour", Guid.NewGuid(), null, now, now.AddHours(2), true, Guid.NewGuid());
        maintenance.Cancel();
        Assert.True(maintenance.IsCancelled);
        Assert.True(maintenance.SuppressAlerts);
    }
}
