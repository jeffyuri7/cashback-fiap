namespace Cashback.Clean.Domain.Rules;

/// <summary>Soma o que cada regra concede. Não conhece nenhuma regra específica.</summary>
public class CashbackCalculator(IEnumerable<ICashbackRule> rules)
{
    public CashbackQuote Calculate(decimal amount, string category, DateTimeOffset purchaseDate)
    {
        var percentage = rules.Sum(rule => rule.PercentageFor(category, purchaseDate));
        return new CashbackQuote(percentage, Math.Round(amount * percentage, 2, MidpointRounding.AwayFromZero));
    }
}

/// <summary>O percentual somado de todas as regras e o cashback que ele gera (antes do teto).</summary>
public record CashbackQuote(decimal Percentage, decimal Amount);
