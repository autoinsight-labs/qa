using AutoInsight.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Yard> Yards => Set<Yard>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Beacon> Beacons => Set<Beacon>();
        public DbSet<YardEmployee> YardEmployees => Set<YardEmployee>();
        public DbSet<EmployeeInvite> EmployeeInvites => Set<EmployeeInvite>();
        public DbSet<YardCapacitySnapshot> YardCapacitySnapshots => Set<YardCapacitySnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<VehicleModel>();
            modelBuilder.HasPostgresEnum<EmployeeRole>();
            modelBuilder.HasPostgresEnum<VehicleStatus>();
            modelBuilder.HasPostgresEnum<InviteStatus>();

            modelBuilder.Entity<Yard>(entity =>
            {
                entity.ToTable("yards");
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Capacity).IsRequired();
            });
            modelBuilder.Entity<YardCapacitySnapshot>(entity =>
            {
                entity.ToTable("yard_capacity_snapshots");
                entity.Property(e => e.CapturedAt).IsRequired();
                entity.Property(e => e.VehiclesInYard).IsRequired();
                entity.Property(e => e.Capacity).IsRequired();

                entity.HasOne(e => e.Yard)
                    .WithMany(y => y.CapacitySnapshots)
                    .HasForeignKey(e => e.YardId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("vehicles");
                entity.Property(v => v.Model).HasColumnType("vehicle_model");
                entity.Property(v => v.Plate).IsRequired();

                entity.Property(e => e.Status).HasColumnType("vehicle_status");
                entity.Property(e => e.EnteredAt);
                entity.Property(e => e.LeftAt);

                entity.HasOne(e => e.Yard)
                      .WithMany(y => y.Vehicles)
                      .HasForeignKey(e => e.YardId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Assignee)
                      .WithMany(y => y.Vehicles)
                      .HasForeignKey(e => e.AssigneeId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

        modelBuilder.Entity<Beacon>(entity =>
        {
            entity.ToTable("beacons");
                entity.Property(b => b.UUID).IsRequired();
            entity.Property(b => b.Major).IsRequired();
            entity.Property(b => b.Minor).IsRequired();

                entity.HasIndex(b => b.UUID).IsUnique();
            entity.HasIndex(b => new { b.Major, b.Minor }).IsUnique();

            entity.HasOne(b => b.Vehicle)
                .WithOne(v => v.Beacon)
                .HasForeignKey<Beacon>(b => b.VehicleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

            modelBuilder.Entity<YardEmployee>(entity =>
            {
                entity.ToTable("yard_employees");
                entity.Property(e => e.Role).HasColumnType("employee_role");
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(128);
                entity.Property(e => e.YardId).IsRequired();

                entity.HasOne(e => e.Yard)
                      .WithMany(y => y.Employees)
                      .HasForeignKey(e => e.YardId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EmployeeInvite>(entity =>
            {
                entity.ToTable("employee_invites");

                entity.Property(e => e.Role).HasColumnType("employee_role");
                entity.Property(e => e.Status).HasColumnType("invite_status");
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.InviterId).IsRequired().HasMaxLength(128);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.HasOne(e => e.Yard)
                      .WithMany(y => y.Invites)
                      .HasForeignKey(e => e.YardId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
