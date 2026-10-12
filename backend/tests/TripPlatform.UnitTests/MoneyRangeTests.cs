using TripPlatform.Domain.ValueObjects;

namespace TripPlatform.UnitTests;

public class MoneyRangeTests
{
    [Fact]
    public void Scale_multiplies_all_bounds()
    {
        var range = new MoneyRange(100, 150, 200, "USD");
        var scaled = range.Scale(1.2m);
        Assert.Equal(120m, scaled.Min);
        Assert.Equal(180m, scaled.Typical);
        Assert.Equal(240m, scaled.Max);
    }

    [Fact]
    public void Constructor_rejects_inverted_range()
    {
        Assert.Throws<ArgumentException>(() => new MoneyRange(200, 150, 100));
    }
}
