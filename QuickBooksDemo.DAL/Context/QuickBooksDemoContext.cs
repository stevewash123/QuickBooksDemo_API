using Microsoft.EntityFrameworkCore;
using QuickBooksDemo.Models.Entities;
using QuickBooksDemo.Models.Enums;
using System.Text.Json;

namespace QuickBooksDemo.DAL.Context;

public class QuickBooksDemoContext : DbContext
{
    public QuickBooksDemoContext(DbContextOptions<QuickBooksDemoContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<LineItem> LineItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to use timestamp without time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerType).HasConversion<string>();
            entity.HasMany(e => e.Jobs).WithOne(e => e.Customer).HasForeignKey(e => e.CustomerId);
        });

        // Technician configuration
        modelBuilder.Entity<Technician>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Specialties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
                )
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            entity.HasMany(e => e.Jobs).WithOne(e => e.AssignedTechnician).HasForeignKey(e => e.AssignedTechnicianId);
        });

        // Job configuration
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.JobType).HasConversion<string>();
            entity.Property(e => e.QuotedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Customer).WithMany(e => e.Jobs).HasForeignKey(e => e.CustomerId);
            entity.HasOne(e => e.AssignedTechnician).WithMany(e => e.Jobs).HasForeignKey(e => e.AssignedTechnicianId);
            entity.HasMany(e => e.LineItems).WithOne(e => e.Job).HasForeignKey(e => e.JobId);
        });

        // LineItem configuration
        modelBuilder.Entity<LineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaterialCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LaborCost).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Job).WithMany(e => e.LineItems).HasForeignKey(e => e.JobId);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = "cust_001",
                Name = "John Smith",
                Email = "john@email.com",
                Phone = "555-0100",
                Address = "123 Main St, Springfield",
                CustomerType = CustomerType.Residential,
                Notes = "Prefers morning appointments"
            },
            new Customer
            {
                Id = "cust_002",
                Name = "Acme Manufacturing",
                Email = "facilities@acme.com",
                Phone = "555-0200",
                Address = "456 Industrial Blvd, Springfield",
                CustomerType = CustomerType.Commercial,
                Notes = "Requires safety check-in at front desk"
            }
        );

        // Seed Technicians
        modelBuilder.Entity<Technician>().HasData(
            new Technician
            {
                Id = "tech_001",
                Name = "Mike Johnson",
                Phone = "555-0301",
                Email = "mike@electricco.com",
                Specialties = new List<string> { "residential", "service_calls" },
                Active = true
            },
            new Technician
            {
                Id = "tech_002",
                Name = "Sarah Chen",
                Phone = "555-0302",
                Email = "sarah@electricco.com",
                Specialties = new List<string> { "commercial", "high_voltage", "installation" },
                Active = true
            }
        );

        // Seed Jobs
        modelBuilder.Entity<Job>().HasData(
            new Job
            {
                Id = "CCE_0001",
                CustomerId = "cust_001",
                Status = JobStatus.Quote,
                JobType = JobType.Installation,
                Description = "Install EV charger in garage",
                QuotedAmount = 1200.00m,
                ActualAmount = null,
                CreatedDate = new DateTime(2025, 10, 15),
                ScheduledDate = null,
                CompletedDate = null,
                AssignedTechnicianId = null
            },
            new Job
            {
                Id = "CCE_0002",
                CustomerId = "cust_002",
                Status = JobStatus.InProgress,
                JobType = JobType.ServiceCall,
                Description = "Emergency - partial power outage in building",
                QuotedAmount = 500.00m,
                ActualAmount = null,
                CreatedDate = new DateTime(2025, 10, 28),
                ScheduledDate = new DateTime(2025, 10, 28),
                CompletedDate = null,
                AssignedTechnicianId = "tech_002"
            },
            new Job
            {
                Id = "CCE_0003",
                CustomerId = "cust_001",
                Status = JobStatus.Completed,
                JobType = JobType.Repair,
                Description = "Replace faulty outlets in kitchen",
                QuotedAmount = 250.00m,
                ActualAmount = 275.00m,
                CreatedDate = new DateTime(2025, 10, 1),
                ScheduledDate = new DateTime(2025, 10, 5),
                CompletedDate = new DateTime(2025, 10, 5),
                AssignedTechnicianId = "tech_001"
            }
        );

        // Seed LineItems
        modelBuilder.Entity<LineItem>().HasData(
            new LineItem
            {
                Id = "item_001",
                JobId = "CCE_0001",
                Description = "Level 2 EV Charger",
                MaterialCost = 600.00m,
                LaborHours = 4,
                LaborCost = 400.00m
            },
            new LineItem
            {
                Id = "item_002",
                JobId = "CCE_0001",
                Description = "Electrical panel upgrade",
                MaterialCost = 150.00m,
                LaborHours = 2,
                LaborCost = 200.00m
            },
            new LineItem
            {
                Id = "item_003",
                JobId = "CCE_0002",
                Description = "Diagnose and repair circuit",
                MaterialCost = 100.00m,
                LaborHours = 3,
                LaborCost = 300.00m
            },
            new LineItem
            {
                Id = "item_004",
                JobId = "CCE_0003",
                Description = "Replace 4 GFCI outlets",
                MaterialCost = 80.00m,
                LaborHours = 2,
                LaborCost = 195.00m
            }
        );
    }
}