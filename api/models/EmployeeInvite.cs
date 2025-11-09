namespace AutoInsight.Models
{
    public enum InviteStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class EmployeeInvite
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string Email { get; set; }
        public required EmployeeRole Role { get; set; }
        public InviteStatus Status { get; set; } = InviteStatus.Pending;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public required string InviterId { get; set; }

        public required Guid YardId { get; init; }
        public required Yard Yard { get; set; }
    }
}
