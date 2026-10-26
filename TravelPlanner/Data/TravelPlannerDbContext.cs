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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}