namespace Cashback.Clean.Domain.Rules;

/// <summary>Campanha "supermercado em dobro": +2% em supermercado durante 2026.</summary>
public class SupermarketDoubleCampaign : ICashbackRule
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public decimal PercentageFor(string category, DateTimeOffset purchaseDate) =>
        category == "supermarket" && purchaseDate >= Start && purchaseDate < End ? 0.02m : 0m;
}
