using AutoInsight.Data;
using AutoInsight.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace AutoInsight.Services;

public interface IYardCapacitySnapshotService
{
    Task CaptureAsync(Yard yard, CancellationToken cancellationToken = default);
}

internal sealed class YardCapacitySnapshotService(AppDbContext dbContext) : IYardCapacitySnapshotService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task CaptureAsync(Yard yard, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yard);

        var activeVehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.YardId == yard.Id && vehicle.LeftAt == null)
            .CountAsync(cancellationToken);

        var snapshot = new YardCapacitySnapshot
        {
            YardId = yard.Id,
            VehiclesInYard = activeVehicles,
            Capacity = yard.Capacity
        };

        _dbContext.YardCapacitySnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
