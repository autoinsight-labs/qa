namespace AutoInsight.Models
{
    public enum VehicleModel
    {
        MottuSport110i,
        Mottue,
        HondaPop110i,
        TVSSport110i
    }

    public enum VehicleStatus
    {
        Scheduled,
        Waiting,
        OnService,
        Finished,
        Cancelled
    }

    public class Vehicle
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public required string Plate { get; set; }
        public required VehicleModel Model { get; set; }

        public required VehicleStatus Status { get; set; }
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }

        public required Guid YardId { get; init; }
        public required Yard Yard { get; set; }

        public Beacon? Beacon { get; set; }

        public Guid? AssigneeId { get; set; }
        public YardEmployee? Assignee { get; set; }
    }
}
