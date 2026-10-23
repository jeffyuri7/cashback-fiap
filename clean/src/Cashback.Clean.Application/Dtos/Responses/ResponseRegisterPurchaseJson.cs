namespace Cashback.Clean.Application.Dtos.Responses;

// campaignBonus (aula 3) é um campo novo no fim: mudança compatível, quem não conhece o campo o ignora (leitor tolerante)
public record ResponseRegisterPurchaseJson(Guid PurchaseId, string Category, decimal Percentage, decimal Cashback, decimal Balance,
    decimal CampaignBonus = 0);
