namespace AutoInsight.Models
{
    public enum EmployeeRole
    {
        Admin,
        Member
    }

    public class YardEmployee
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string Name { get; set; }
        public string? ImageUrl { get; set; }
        public required EmployeeRole Role { get; set; }
        public required string UserId { get; init; }

        public required Guid YardId { get; set; }
        public required Yard Yard { get; set; }

        public ICollection<Vehicle> Vehicles { get; } = new List<Vehicle>();
    }
}
