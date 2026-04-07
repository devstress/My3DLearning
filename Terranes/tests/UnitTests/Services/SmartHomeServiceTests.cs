using Microsoft.Extensions.Logging.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;
using Terranes.PartnerIntegration;

namespace Terranes.UnitTests.Services;

[TestFixture]
public sealed class SmartHomeServiceTests
{
    private SmartHomeService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SmartHomeService(NullLogger<SmartHomeService>.Instance);

    // ── 1. Device Catalog ──

    [Test]
    public async Task AddDeviceAsync_ValidDevice_ReturnsWithGeneratedId()
    {
        var device = MakeDevice();
        var created = await _sut.AddDeviceAsync(device);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo("Smart Thermostat Pro"));
    }

    [Test]
    public void AddDeviceAsync_EmptyName_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddDeviceAsync(MakeDevice() with { Name = "" }));
    }

    [Test]
    public void AddDeviceAsync_NegativePrice_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddDeviceAsync(MakeDevice() with { PriceAud = -1m }));
    }

    [Test]
    public void AddDeviceAsync_EmptySku_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddDeviceAsync(MakeDevice() with { Sku = "" }));
    }

    [Test]
    public void AddDeviceAsync_EmptyProtocol_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "" }));
    }

    [Test]
    public async Task GetDeviceAsync_ExistingDevice_ReturnsDevice()
    {
        var created = await _sut.AddDeviceAsync(MakeDevice());
        var retrieved = await _sut.GetDeviceAsync(created.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Category, Is.EqualTo(SmartHomeCategory.Climate));
    }

    [Test]
    public async Task GetDeviceAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetDeviceAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ── 2. Search & Filtering ──

    [Test]
    public async Task SearchDevicesAsync_ByCategory_FiltersCorrectly()
    {
        await _sut.AddDeviceAsync(MakeDevice() with { Category = SmartHomeCategory.Climate });
        await _sut.AddDeviceAsync(MakeDevice() with { Category = SmartHomeCategory.Security, Sku = "SH-SEC-001" });

        var results = await _sut.SearchDevicesAsync(category: SmartHomeCategory.Climate);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchDevicesAsync_ByProtocol_FiltersCorrectly()
    {
        await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Zigbee" });
        await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Z-Wave", Sku = "SH-ZW-001" });

        var results = await _sut.SearchDevicesAsync(protocol: "Zigbee");
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchDevicesAsync_ByMaxPrice_FiltersCorrectly()
    {
        await _sut.AddDeviceAsync(MakeDevice() with { PriceAud = 200m });
        await _sut.AddDeviceAsync(MakeDevice() with { PriceAud = 600m, Sku = "SH-EXP-001" });

        var results = await _sut.SearchDevicesAsync(maxPrice: 300m);
        Assert.That(results, Has.Count.EqualTo(1));
    }

    // ── 3. Compatibility & Pricing ──

    [Test]
    public async Task CheckCompatibilityAsync_SameProtocol_ReturnsTrue()
    {
        var d1 = await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Zigbee" });
        var d2 = await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Zigbee", Sku = "SH-CLI-002" });

        var compatible = await _sut.CheckCompatibilityAsync(d1.Id, d2.Id);
        Assert.That(compatible, Is.True);
    }

    [Test]
    public async Task CheckCompatibilityAsync_DifferentProtocol_ReturnsFalse()
    {
        var d1 = await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Zigbee" });
        var d2 = await _sut.AddDeviceAsync(MakeDevice() with { CompatibilityProtocol = "Z-Wave", Sku = "SH-ZW-001" });

        var compatible = await _sut.CheckCompatibilityAsync(d1.Id, d2.Id);
        Assert.That(compatible, Is.False);
    }

    [Test]
    public void CheckCompatibilityAsync_NonExistentDevice_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckCompatibilityAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task CalculatePackagePriceAsync_MultipleDevices_SumsCorrectly()
    {
        var d1 = await _sut.AddDeviceAsync(MakeDevice() with { PriceAud = 300m });
        var d2 = await _sut.AddDeviceAsync(MakeDevice() with { PriceAud = 150m, Sku = "SH-CLI-002" });
        var d3 = await _sut.AddDeviceAsync(MakeDevice() with { PriceAud = 450m, Sku = "SH-CLI-003" });

        var total = await _sut.CalculatePackagePriceAsync([d1.Id, d2.Id, d3.Id]);
        Assert.That(total, Is.EqualTo(900m));
    }

    [Test]
    public void CalculatePackagePriceAsync_NonExistentDevice_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CalculatePackagePriceAsync([Guid.NewGuid()]));
    }

    [Test]
    public async Task CalculatePackagePriceAsync_EmptyList_ReturnsZero()
    {
        var total = await _sut.CalculatePackagePriceAsync([]);
        Assert.That(total, Is.EqualTo(0m));
    }

    private static SmartHomeDevice MakeDevice() => new(
        Guid.Empty, Guid.NewGuid(), "Smart Thermostat Pro", SmartHomeCategory.Climate,
        349.99m, "Zigbee", true, "SH-CLI-001");
}
