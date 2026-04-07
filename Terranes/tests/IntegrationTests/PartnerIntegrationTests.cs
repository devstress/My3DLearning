using System.Net;
using System.Net.Http.Json;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.IntegrationTests;

/// <summary>
/// Integration tests for partner services exercised through the HTTP API.
/// Covers: Furniture and Smart Home endpoints (catalog-based, no multi-body binding).
/// Partner registration endpoints are covered by unit tests because they use multi-body parameter binding.
/// </summary>
[TestFixture]
public sealed class PartnerIntegrationTests : IntegrationTestBase
{
    // ── 1. Furniture ──

    [Test]
    public async Task Furniture_AddItem_AndRetrieve()
    {
        var item = MakeFurnitureItem();
        var created = await PostAsync<FurnitureItem>("/api/furniture/items", item);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.Name, Is.EqualTo(item.Name));

        var retrieved = await GetAsync<FurnitureItem>($"/api/furniture/items/{created.Id}");
        Assert.That(retrieved.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Furniture_SearchCatalog_ReturnsByCategory()
    {
        var item = MakeFurnitureItem();
        await PostAsync<FurnitureItem>("/api/furniture/items", item);

        var results = await GetAsync<List<FurnitureItem>>($"/api/furniture/items?category={item.Category}");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Furniture_FitItem_AndGetFittings()
    {
        var item = MakeFurnitureItem();
        var created = await PostAsync<FurnitureItem>("/api/furniture/items", item);

        var homeModelId = Guid.NewGuid();
        var fitting = new RoomFitting(Guid.Empty, homeModelId, created.Id, "Living Room", 3.0, 4.0, 0.0);
        var createdFitting = await PostAsync<RoomFitting>("/api/furniture/fittings", fitting);

        Assert.That(createdFitting.Id, Is.Not.EqualTo(Guid.Empty));

        var fittings = await GetAsync<List<RoomFitting>>($"/api/furniture/fittings/model/{homeModelId}");
        Assert.That(fittings, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Furniture_CalculateTotal_ReturnsTotalPrice()
    {
        var item = MakeFurnitureItem() with { PriceAud = 1500m };
        var created = await PostAsync<FurnitureItem>("/api/furniture/items", item);

        var homeModelId = Guid.NewGuid();
        await PostAsync<RoomFitting>("/api/furniture/fittings",
            new RoomFitting(Guid.Empty, homeModelId, created.Id, "Bedroom", 1.0, 2.0, 0.0));

        var response = await Client.GetAsync($"/api/furniture/total/{homeModelId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.That(body, Does.ContainKey("totalAud"));
    }

    // ── 2. Smart Home ──

    [Test]
    public async Task SmartHome_AddDevice_AndRetrieve()
    {
        var device = MakeSmartHomeDevice();
        var created = await PostAsync<SmartHomeDevice>("/api/smart-home/devices", device);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var retrieved = await GetAsync<SmartHomeDevice>($"/api/smart-home/devices/{created.Id}");
        Assert.That(retrieved.Name, Is.EqualTo(device.Name));
    }

    [Test]
    public async Task SmartHome_SearchDevices_ByCategory()
    {
        await PostAsync<SmartHomeDevice>("/api/smart-home/devices", MakeSmartHomeDevice());
        var results = await GetAsync<List<SmartHomeDevice>>("/api/smart-home/devices?category=Lighting");
        Assert.That(results, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task SmartHome_CheckCompatibility_ReturnsBoolResult()
    {
        var d1 = await PostAsync<SmartHomeDevice>("/api/smart-home/devices", MakeSmartHomeDevice());
        var d2 = await PostAsync<SmartHomeDevice>("/api/smart-home/devices", MakeSmartHomeDevice());

        var response = await Client.GetAsync($"/api/smart-home/compatibility?deviceId1={d1.Id}&deviceId2={d2.Id}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.That(body, Does.ContainKey("compatible"));
    }

    [Test]
    public async Task SmartHome_CalculatePackagePrice_ReturnsTotalPrice()
    {
        var d1 = await PostAsync<SmartHomeDevice>("/api/smart-home/devices", MakeSmartHomeDevice() with { PriceAud = 200m });
        var d2 = await PostAsync<SmartHomeDevice>("/api/smart-home/devices", MakeSmartHomeDevice() with { PriceAud = 300m });

        var response = await Client.PostAsJsonAsync("/api/smart-home/package-price", new List<Guid> { d1.Id, d2.Id });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.That(body, Does.ContainKey("totalAud"));
    }

    [Test]
    public async Task Furniture_GetNonExistentItem_Returns404()
    {
        var response = await Client.GetAsync($"/api/furniture/items/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task SmartHome_GetNonExistentDevice_Returns404()
    {
        var response = await Client.GetAsync($"/api/smart-home/devices/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ── Factory Methods ──

    private static FurnitureItem MakeFurnitureItem() => new(
        Guid.Empty, Guid.NewGuid(), "Modern Sofa", FurnitureCategory.LivingRoom,
        2500m, 2.2, 0.9, 0.85, "SOFA-001", true);

    private static SmartHomeDevice MakeSmartHomeDevice() => new(
        Guid.Empty, Guid.NewGuid(), "Smart Dimmer Switch", SmartHomeCategory.Lighting,
        89m, "Zigbee", false, "SHS-001");
}
