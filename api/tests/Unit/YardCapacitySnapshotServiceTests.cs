using System;
using System.Threading.Tasks;
using AutoInsight.Data;
using AutoInsight.Models;
using AutoInsight.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoInsight.Tests.Unit;

public sealed class YardCapacitySnapshotServiceTests
{
    public static TheoryData<int, int> SnapshotScenarios => new()
    {
        { 0, 2 },
        { 3, 1 }
    };

    [Theory]
    [MemberData(nameof(SnapshotScenarios))]
    public async Task CaptureAsync_StoresSnapshotWithActiveVehicleCount(int activeVehicles, int inactiveVehicles)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var yard = new Yard
        {
            Name = "Test Yard",
            OwnerId = "owner-1",
            Capacity = 25
        };

        context.Yards.Add(yard);

        for (var i = 0; i < activeVehicles; i++)
        {
            var vehicle = new Vehicle
            {
                Plate = $"AAA{i}123",
                Model = VehicleModel.MottuSport110i,
                Status = VehicleStatus.Waiting,
                Yard = yard,
                YardId = yard.Id,
                Beacon = null!
            };

            vehicle.Beacon = new Beacon
            {
                UUID = $"active-uuid-{i}",
                Major = $"active-major-{i}",
                Minor = $"active-minor-{i}",
                VehicleId = vehicle.Id,
                Vehicle = vehicle
            };

            context.Vehicles.Add(vehicle);
        }

        for (var i = 0; i < inactiveVehicles; i++)
        {
            var vehicle = new Vehicle
            {
                Plate = $"BBB{i}456",
                Model = VehicleModel.MottuSport110i,
                Status = VehicleStatus.Finished,
                LeftAt = DateTime.UtcNow,
                Yard = yard,
                YardId = yard.Id,
                Beacon = null!
            };

            vehicle.Beacon = new Beacon
            {
                UUID = $"inactive-uuid-{i}",
                Major = $"inactive-major-{i}",
                Minor = $"inactive-minor-{i}",
                VehicleId = vehicle.Id,
                Vehicle = vehicle
            };

            context.Vehicles.Add(vehicle);
        }

        await context.SaveChangesAsync();

        var sut = new YardCapacitySnapshotService(context);
        await sut.CaptureAsync(yard);

        var snapshot = await context.YardCapacitySnapshots.SingleAsync();
        Assert.Equal(yard.Id, snapshot.YardId);
        Assert.Equal(yard.Capacity, snapshot.Capacity);
        Assert.Equal(activeVehicles, snapshot.VehiclesInYard);
    }

    [Fact]
    public async Task CaptureAsync_ThrowsWhenYardIsNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var sut = new YardCapacitySnapshotService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CaptureAsync(null!));
    }
}
