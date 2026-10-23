namespace Cashback.Clean.Domain.Rules;

/// <summary>Soma o que cada regra concede. Não conhece nenhuma regra específica.</summary>
public class CashbackCalculator(IEnumerable<ICashbackRule> rules)
{
    /// <param name="enabledFlags">As flags ligadas para quem comprou (aula 3). Regra com flag desligada não entra na conta.</param>
    public CashbackQuote Calculate(decimal amount, string category, DateTimeOffset purchaseDate,
        IReadOnlySet<string>? enabledFlags = null)
    {
        var applied = rules
            .Where(rule => rule.Flag is null || (enabledFlags?.Contains(rule.Flag) ?? false))
            .ToList();
        var percentage = applied.Sum(rule => rule.PercentageFor(category, purchaseDate));
        // o que vem das campanhas com flag (a Black Friday), separado do cashback base
        var bonusPercentage = applied.Where(rule => rule.Flag is not null).Sum(rule => rule.PercentageFor(category, purchaseDate));
        return new CashbackQuote(percentage, Round(amount * percentage), Round(amount * bonusPercentage));
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>O percentual somado de todas as regras e o cashback que ele gera (antes do teto), com a parte que veio de campanha com flag.</summary>
public record CashbackQuote(decimal Percentage, decimal Amount, decimal CampaignBonus = 0);
