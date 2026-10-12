namespace TripPlatform.Domain.ValueObjects;

public sealed class MoneyRange
{
    public decimal Min { get; private set; }
    public decimal Typical { get; private set; }
    public decimal Max { get; private set; }
    public string Currency { get; private set; } = "USD";

    private MoneyRange() { }

    public MoneyRange(decimal min, decimal typical, decimal max, string currency = "USD")
    {
        if (min < 0 || typical < 0 || max < 0)
            throw new ArgumentOutOfRangeException(nameof(min), "Money values cannot be negative.");
        if (min > typical || typical > max)
            throw new ArgumentException("Money range must satisfy Min <= Typical <= Max.");

        Min = min;
        Typical = typical;
        Max = max;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
    }

    public MoneyRange Scale(decimal multiplier) =>
        new(Round(Min * multiplier), Round(Typical * multiplier), Round(Max * multiplier), Currency);

    public MoneyRange Add(MoneyRange other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot add money ranges with different currencies.");

        return new MoneyRange(Min + other.Min, Typical + other.Typical, Max + other.Max, Currency);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
