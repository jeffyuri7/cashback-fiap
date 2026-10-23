namespace Cashback.Clean.Domain.Rules;

/// <summary>A regra base: o percentual de cada categoria.</summary>
public class CategoryRule : ICashbackRule
{
    public decimal PercentageFor(string category, DateTimeOffset purchaseDate) => category switch
    {
        "supermarket" => 0.02m,
        "fuel" => 0.03m,
        _ => 0.01m
    };
}
