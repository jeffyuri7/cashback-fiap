namespace Cashback.Clean.Domain.Entities;

/// <summary>Resgate do saldo, enviado via PIX para a chave do cliente.</summary>
public record Redemption(Guid Id, string CustomerId, decimal Amount, string PixKey, DateTimeOffset CreatedAt);
