namespace Cashback.Clean.Application.Dtos.Responses;

public record ResponseRegisterPurchaseJson(Guid PurchaseId, string Category, decimal Percentage, decimal Cashback, decimal Balance);
