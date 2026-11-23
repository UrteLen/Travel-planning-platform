using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;

namespace TravelPlanner.UnitTests;

// Each test gets its own in-memory database so tests never share data.
// Needs the Microsoft.EntityFrameworkCore.InMemory package in TravelPlanner.UnitTests.csproj.
internal static class TestDb
{
    public static TravelPlannerDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TravelPlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TravelPlannerDbContext(options);
    }
}
