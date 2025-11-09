namespace AutoInsight.Models;

public class YardCapacitySnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid YardId { get; init; }
    public Yard Yard { get; set; } = default!;

    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
    public int VehiclesInYard { get; init; }
    public int Capacity { get; init; }
}
