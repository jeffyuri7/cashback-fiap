using Cashback.Clean.Domain.Rules;

namespace Cashback.Clean.Tests.Domain;

public class PharmacyCampaignTests
{
    private static readonly DateTimeOffset October = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static CashbackCalculator CreateCalculator() =>
        new([new CategoryRule(), new SupermarketDoubleCampaign(), new PharmacyCampaign()]);

    [Fact]
    public void Pharmacy_during_campaign_gets_6_percent() =>
        Assert.Equal(0.06m, CreateCalculator().Calculate(100m, "pharmacy", October).Percentage);

    [Fact]
    public void Other_categories_do_not_change() =>
        Assert.Equal(0.04m, CreateCalculator().Calculate(100m, "supermarket", October).Percentage);
}
