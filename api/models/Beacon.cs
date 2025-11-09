namespace AutoInsight.Models
{
    public class Beacon
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public required string UUID { get; set; }
        public required string Major { get; set; }
        public required string Minor { get; set; }

        public required Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
    }
}
