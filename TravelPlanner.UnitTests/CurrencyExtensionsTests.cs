using TravelPlanner.Extensions;

namespace TravelPlanner.UnitTests;

public class CurrencyExtensionsTests
{
    [Theory]
    [InlineData(100, 3, 33.33)]
    [InlineData(10, 4, 2.5)]
    [InlineData(0.01, 2, 0.01)]
    [InlineData(50, 1, 50)]
    public void ToShare_RoundsToTwoDecimalsAwayFromZero(double total, int people, double expected)
    {
        Assert.Equal((decimal)expected, ((decimal)total).ToShare(people));
    }

    [Fact]
    public void ToCurrencyString_UsesLithuanianCultureByDefault()
    {
        var text = 12.5m.ToCurrencyString();

        Assert.Contains("12,50", text);
        Assert.Contains("€", text);
    }

    [Fact]
    public void ToCurrencyString_UsesTheRequestedCulture()
    {
        var text = 12.5m.ToCurrencyString("en-US");

        Assert.Contains("12.50", text);
        Assert.Contains("$", text);
    }
}
