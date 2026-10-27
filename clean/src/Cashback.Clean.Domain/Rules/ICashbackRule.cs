namespace Cashback.Clean.Domain.Rules;

/// <summary>
/// Cada regra devolve o percentual que ela soma ao cashback de uma compra.
/// Campanha nova = classe nova (aberto para extensão, fechado para modificação).
/// </summary>
public interface ICashbackRule
{
    decimal PercentageFor(string category, DateTimeOffset purchaseDate);
}
