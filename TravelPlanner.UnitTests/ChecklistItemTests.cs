using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.UnitTests;

public class ChecklistItemTests
{
    [Fact]
    public void Constructor_WhenTextIsWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ChecklistItem(Guid.NewGuid(), "   "));
    }

    [Fact]
    public void Constructor_WhenTextIsLongerThanLimit_ThrowsArgumentException()
    {
        var tooLongText = new string('a', ChecklistItem.MaxTextLength + 1);

        Assert.Throws<ArgumentException>(() => new ChecklistItem(Guid.NewGuid(), tooLongText));
    }

    [Fact]
    public void Constructor_WhenCategoryIsUnknown_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ChecklistItem(Guid.NewGuid(), "Passport", (ChecklistCategory)99));
    }

    [Fact]
    public void Constructor_TrimsText()
    {
        var item = new ChecklistItem(Guid.NewGuid(), "  Passport  ");

        Assert.Equal("Passport", item.Text);
    }

    [Fact]
    public void Constructor_WhenCategoryIsOmitted_UsesOther()
    {
        var item = new ChecklistItem(Guid.NewGuid(), "Passport");

        Assert.Equal(ChecklistCategory.Other, item.Category);
        Assert.False(item.IsDone);
    }

    [Fact]
    public void MarkDone_ThenMarkNotDone_TogglesIsDone()
    {
        var item = new ChecklistItem(Guid.NewGuid(), "Passport");

        item.MarkDone();
        Assert.True(item.IsDone);

        item.MarkNotDone();
        Assert.False(item.IsDone);
    }
}