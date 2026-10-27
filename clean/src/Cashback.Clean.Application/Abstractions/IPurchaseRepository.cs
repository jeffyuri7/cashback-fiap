using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.Abstractions;

public interface IPurchaseRepository
{
    Task Add(Purchase purchase);

    Task<Purchase?> Get(Guid id);

    // consulta pronta para exibição (com o nome da loja): um "read model" simples
    Task<IList<PurchaseSummary>> ListByCustomer(string customerId);
}

public record PurchaseSummary(Guid Id, string StoreName, decimal Amount, decimal Percentage, decimal Cashback, DateTime CreatedAt);
