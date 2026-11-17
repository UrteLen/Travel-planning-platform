using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ChecklistServiceTests
{
    private static ChecklistItem CreateItem(
        string text,
        ChecklistCategory category = ChecklistCategory.Other,
        bool isDone = false)
    {
        var item = new ChecklistItem(Guid.NewGuid(), text, category);

        if (isDone)
        {
            item.MarkDone();
        }

        return item;
    }

    [Fact]
    public void CalculatePercent_WhenThereAreNoItems_ReturnsZero()
    {
        Assert.Equal(0, ChecklistService.CalculatePercent(0, 0));
    }

    [Fact]
    public void CalculatePercent_RoundsToNearestWholeNumber()
    {
        Assert.Equal(33, ChecklistService.CalculatePercent(1, 3));
        Assert.Equal(67, ChecklistService.CalculatePercent(2, 3));
        Assert.Equal(100, ChecklistService.CalculatePercent(3, 3));
    }

    [Fact]
    public void BuildResponse_WhenListIsEmpty_ReturnsZeroes()
    {
        var response = ChecklistService.BuildResponse(new List<ChecklistItem>());

        Assert.Empty(response.Groups);
        Assert.Equal(0, response.DoneCount);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.PercentDone);
    }

    [Fact]
    public void BuildResponse_CountsDoneItemsAndPercent()
    {
        var items = new[]
        {
            CreateItem("Passport", ChecklistCategory.Documents, isDone: true),
            CreateItem("Raincoat", ChecklistCategory.Clothes, isDone: true),
            CreateItem("Medicine", ChecklistCategory.Health)
        };

        var response = ChecklistService.BuildResponse(items);

        Assert.Equal(2, response.DoneCount);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(67, response.PercentDone);
    }

    [Fact]
    public void BuildResponse_GroupsByCategoryInEnumOrder()
    {
        var items = new[]
        {
            CreateItem("Sunscreen"),
            CreateItem("Passport", ChecklistCategory.Documents),
            CreateItem("Raincoat", ChecklistCategory.Clothes)
        };

        var response = ChecklistService.BuildResponse(items);

        var categories = response.Groups.Select(group => group.Category).ToList();
        Assert.Equal(
            new[] { ChecklistCategory.Documents, ChecklistCategory.Clothes, ChecklistCategory.Other },
            categories);
    }

    [Fact]
    public void BuildResponse_PutsUnfinishedItemsBeforeFinishedOnesInsideGroup()
    {
        var items = new[]
        {
            CreateItem("Insurance", ChecklistCategory.Documents, isDone: true),
            CreateItem("Passport", ChecklistCategory.Documents)
        };

        var response = ChecklistService.BuildResponse(items);

        var group = Assert.Single(response.Groups);
        Assert.Equal(new[] { "Passport", "Insurance" }, group.Items.Select(item => item.Text));
    }
}