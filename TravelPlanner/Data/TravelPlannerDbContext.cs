using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.Data;

public class TravelPlannerDbContext : DbContext
{
    public TravelPlannerDbContext(DbContextOptions<TravelPlannerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<StoredTripVersion> TripVersions => Set<StoredTripVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Our models create their own Guid ids, so a set Id does not mean "already in the database".
        modelBuilder.Entity<Trip>().Property(t => t.Id).ValueGeneratedNever();
        modelBuilder.Entity<Participant>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<Budget>().Property(b => b.Id).ValueGeneratedNever();
        modelBuilder.Entity<ExpenseEntry>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<TripActivity>().Property(a => a.Id).ValueGeneratedNever();
        modelBuilder.Entity<ScheduledActivity>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<PlannedVisit>().Property(v => v.Id).ValueGeneratedNever();
        modelBuilder.Entity<Place>().Property(p => p.Id).ValueGeneratedNever();

        // Dictionaries are stored as JSON columns.
        modelBuilder.Entity<Budget>()
            .Property(b => b.CategoryLimits)
            .HasColumnType("jsonb")
            .HasConversion(
                limits => JsonSerializer.Serialize(limits, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<Category, decimal>>(json, (JsonSerializerOptions?)null)
                        ?? new Dictionary<Category, decimal>(),
                new ValueComparer<Dictionary<Category, decimal>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    d => d.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    d => new Dictionary<Category, decimal>(d)));

        modelBuilder.Entity<TripActivity>()
            .Property(a => a.Votes)
            .HasColumnType("jsonb")
            .HasConversion(
                votes => JsonSerializer.Serialize(votes, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<Guid, VoteType>>(json, (JsonSerializerOptions?)null)
                        ?? new Dictionary<Guid, VoteType>(),
                new ValueComparer<Dictionary<Guid, VoteType>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    d => d.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    d => new Dictionary<Guid, VoteType>(d)));

        // GeoLocation is stored as two columns on the same row.
        modelBuilder.Entity<Place>().ComplexProperty(p => p.Location, location =>
        {
            location.Property(l => l.Latitude);
            location.Property(l => l.Longitude);
        });

        modelBuilder.Entity<TripActivity>().ComplexProperty(a => a.Location, location =>
        {
            location.Property(l => l.Latitude);
            location.Property(l => l.Longitude);
        });

        // Every child belongs to exactly one parent, so removing it from the list deletes the row.
        modelBuilder.Entity<Trip>().HasMany(t => t.Participants).WithOne().IsRequired();
        modelBuilder.Entity<Trip>().HasMany(t => t.Activities).WithOne().IsRequired();
        modelBuilder.Entity<Trip>().HasMany(t => t.ScheduledActivities).WithOne().IsRequired();
        modelBuilder.Entity<Trip>().HasMany(t => t.Plan).WithOne().IsRequired();
        modelBuilder.Entity<Budget>().HasMany(b => b.Expenses).WithOne().IsRequired();

        // Version history: snapshots are JSON, and each version belongs to a trip.
        modelBuilder.Entity<StoredTripVersion>().Property(v => v.Id).ValueGeneratedNever();
        modelBuilder.Entity<StoredTripVersion>().Property(v => v.BudgetJson).HasColumnType("jsonb");
        modelBuilder.Entity<StoredTripVersion>().Property(v => v.PlanJson).HasColumnType("jsonb");
        modelBuilder.Entity<StoredTripVersion>().Property(v => v.ActivitiesJson).HasColumnType("jsonb");
        modelBuilder.Entity<StoredTripVersion>().HasOne<Trip>().WithMany().HasForeignKey(v => v.TripId);
    }
}