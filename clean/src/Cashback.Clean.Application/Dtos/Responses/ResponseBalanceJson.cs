namespace Cashback.Clean.Application.Dtos.Responses;

public record ResponseBalanceJson(string CustomerId, string Name, decimal Balance, decimal CreditedThisMonth, decimal MonthlyLimit);
