using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;

namespace TravelPlanner.UnitTests;

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
