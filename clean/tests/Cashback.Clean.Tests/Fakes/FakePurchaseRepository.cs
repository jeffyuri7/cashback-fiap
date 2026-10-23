using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Tests.Fakes;

public class FakePurchaseRepository : IPurchaseRepository
{
    public List<Purchase> Added { get; } = new();

    public Task Add(Purchase purchase)
    {
        Added.Add(purchase);
        return Task.CompletedTask;
    }

    public Task<Purchase?> Get(Guid id) => Task.FromResult(Added.FirstOrDefault(p => p.Id == id));

    public Task<IList<PurchaseSummary>> ListByCustomer(string customerId) => Task.FromResult<IList<PurchaseSummary>>([]);
}
