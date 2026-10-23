namespace Cashback.Clean.Domain.Entities;

/// <summary>Loja parceira: a categoria dela define o percentual de cashback.</summary>
public record Store(string Id, string Name, string Category);
