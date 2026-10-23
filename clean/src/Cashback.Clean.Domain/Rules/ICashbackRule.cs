namespace Cashback.Clean.Domain.Rules;

/// <summary>
/// Cada regra devolve o percentual que ela soma ao cashback de uma compra.
/// Campanha nova = classe nova (aberto para extensão, fechado para modificação).
/// </summary>
public interface ICashbackRule
{
    decimal PercentageFor(string category, DateTimeOffset purchaseDate);

    /// <summary>
    /// A feature flag que liga a regra (aula 3). Sem flag (o padrão), a regra vale sempre.
    /// Com flag, a regra vai para produção desligada e só vale quando a flag estiver ligada para aquele cliente.
    /// </summary>
    string? Flag => null;
}
