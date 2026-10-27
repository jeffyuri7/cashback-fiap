namespace Cashback.Clean.Application.Dtos.Responses;

public record ResponseCustomerJson(string Id, string Name, decimal Balance);

public record ResponseStoreJson(string Id, string Name, string Category);

public record ResponsePurchaseJson(Guid Id, string StoreName, decimal Amount, decimal Percentage, decimal Cashback, DateTime CreatedAt);
