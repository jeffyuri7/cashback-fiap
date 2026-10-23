using Cashback.Clean.Domain.Rules;

namespace Cashback.Clean.Tests.Domain;

public class CashbackCalculatorTests
{
    private static readonly DateTimeOffset In2026 = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset In2027 = new(2027, 2, 10, 12, 0, 0, TimeSpan.Zero);

    private static CashbackCalculator CreateCalculator() =>
        new([new CategoryRule(), new SupermarketDoubleCampaign()]);

    [Fact]
    public void Supermarket_during_campaign_gets_4_percent()
    {
        var cashback = CreateCalculator().Calculate(250m, "supermarket", In2026).Amount;

        Assert.Equal(10m, cashback);
    }

    [Fact]
    public void Supermarket_after_campaign_gets_2_percent()
    {
        var cashback = CreateCalculator().Calculate(250m, "supermarket", In2027).Amount;

        Assert.Equal(5m, cashback);
    }

    [Fact]
    public void Fuel_gets_3_percent()
    {
        var cashback = CreateCalculator().Calculate(100m, "fuel", In2026).Amount;

        Assert.Equal(3m, cashback);
    }

    [Fact]
    public void Other_categories_get_1_percent()
    {
        var cashback = CreateCalculator().Calculate(59.90m, "pharmacy", In2026).Amount;

        Assert.Equal(0.60m, cashback);
    }
}
