namespace Cashback.Clean.Domain.Rules;

/// <summary>Campanha da farmácia: +5% em farmácia até o fim de 2026.</summary>
public class PharmacyCampaign : ICashbackRule
{
    private static readonly DateTimeOffset End = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public decimal PercentageFor(string category, DateTimeOffset purchaseDate) =>
        category == "pharmacy" && purchaseDate < End ? 0.05m : 0m;
}
