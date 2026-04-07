using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class SmartHomeEndpoints
{
    public static void MapSmartHomeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/smart-home").WithTags("Smart Home");

        group.MapPost("/devices", async (SmartHomeDevice device, ISmartHomeService service) =>
        {
            var created = await service.AddDeviceAsync(device);
            return Results.Created($"/api/smart-home/devices/{created.Id}", created);
        }).WithName("AddSmartHomeDevice");

        group.MapGet("/devices/{deviceId:guid}", async (Guid deviceId, ISmartHomeService service) =>
        {
            var device = await service.GetDeviceAsync(deviceId);
            return device is not null ? Results.Ok(device) : Results.NotFound();
        }).WithName("GetSmartHomeDevice");

        group.MapGet("/devices", async (SmartHomeCategory? category, string? protocol, decimal? maxPrice, ISmartHomeService service) =>
        {
            var results = await service.SearchDevicesAsync(category, protocol, maxPrice);
            return Results.Ok(results);
        }).WithName("SearchSmartHomeDevices");

        group.MapGet("/compatibility", async (Guid deviceId1, Guid deviceId2, ISmartHomeService service) =>
        {
            var compatible = await service.CheckCompatibilityAsync(deviceId1, deviceId2);
            return Results.Ok(new { DeviceId1 = deviceId1, DeviceId2 = deviceId2, Compatible = compatible });
        }).WithName("CheckDeviceCompatibility");

        group.MapPost("/package-price", async (List<Guid> deviceIds, ISmartHomeService service) =>
        {
            var total = await service.CalculatePackagePriceAsync(deviceIds);
            return Results.Ok(new { DeviceCount = deviceIds.Count, TotalAud = total });
        }).WithName("CalculatePackagePrice");
    }
}
