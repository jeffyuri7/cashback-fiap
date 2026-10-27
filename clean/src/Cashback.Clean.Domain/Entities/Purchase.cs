namespace Cashback.Clean.Domain.Entities;

public record Purchase(
    Guid Id, string CustomerId, string StoreId, decimal Amount, string Category,
    decimal Percentage, decimal Cashback, DateTimeOffset CreatedAt);
