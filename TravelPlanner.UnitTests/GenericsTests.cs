using TravelPlanner.Enums;
using TravelPlanner.Extensions;
using TravelPlanner.Models;

namespace TravelPlanner.UnitTests;

public class GenericsTests
{
    [Theory]
    [InlineData("Food", Category.Food)]
    [InlineData("food", Category.Food)]
    [InlineData("TRANSPORT", Category.Transport)]
    public void TryParseDefined_ValidName_ReturnsTrue(string text, Category expected)
    {
        var ok = EnumParser<Category>.TryParseDefined(text, out var result);

        Assert.True(ok);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseDefined_InvalidValue_ReturnsFalse(string? text)
    {
        Assert.False(EnumParser<Category>.TryParseDefined(text, out _));
    }

    [Fact]
    public void TryParseDefined_WorksWithOtherEnum()
    {
        Assert.True(EnumParser<BudgetStatus>.TryParseDefined("warning", out var status));
        Assert.Equal(BudgetStatus.Warning, status);
    }

    [Fact]
    public void FindById_Participants_ReturnsMatching()
    {
        var first = new Participant { Name = "A" };
        var second = new Participant { Name = "B" };
        var list = new List<Participant> { first, second };

        Assert.Same(second, list.FindById(second.Id));
    }

    [Fact]
    public void FindById_Activities_ReturnsMatching()
    {
        var activity = new TripActivity("Museum", Category.Activities, new GeoLocation(54.68, 25.28), TimeSpan.FromHours(1));
        var list = new List<TripActivity> { activity };

        Assert.Same(activity, list.FindById(activity.Id));
    }

    [Fact]
    public void FindById_UnknownId_ReturnsNull()
    {
        var list = new List<Participant> { new() { Name = "A" } };

        Assert.Null(list.FindById(Guid.NewGuid()));
    }
}