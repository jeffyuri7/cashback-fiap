namespace Cashback.Clean.Domain.Entities;

/// <param name="Cashback">o total creditado</param>
/// <param name="CampaignBonus">a parte do total que veio da campanha com flag (aula 3: a Black Friday)</param>
public record Purchase(
    Guid Id, string CustomerId, string StoreId, decimal Amount, string Category,
    decimal Percentage, decimal Cashback, DateTimeOffset CreatedAt, decimal CampaignBonus = 0)
{
    public decimal CashbackBase => Cashback - CampaignBonus;
}
