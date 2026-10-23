namespace Cashback.Clean.Application.Dtos.Responses;

public record ResponseCustomerJson(string Id, string Name, decimal Balance);

public record ResponseStoreJson(string Id, string Name, string Category);

// campaignBonus (aula 3): campo novo no fim, a parte do cashback que veio da Black Friday
public record ResponsePurchaseJson(Guid Id, string StoreName, decimal Amount, decimal Percentage, decimal Cashback, DateTime CreatedAt,
    decimal CampaignBonus = 0);
