namespace AutoInsight.Models
{
    public class Yard
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public required string Name { get; set; }
        public required string OwnerId { get; set; }
        public int Capacity { get; set; }

        public ICollection<YardEmployee> Employees { get; } = new List<YardEmployee>();
        public ICollection<Vehicle> Vehicles { get; } = new List<Vehicle>();
        public ICollection<EmployeeInvite> Invites { get; } = new List<EmployeeInvite>();
        public ICollection<YardCapacitySnapshot> CapacitySnapshots { get; } = new List<YardCapacitySnapshot>();
    }
}
