using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Tests.Fakes;

public class FakeStoreRepository : IStoreRepository
{
    private readonly List<Store> _stores =
    [
        new("s-mercado", "Mercado", "supermarket"),
        new("s-farmacia", "Farmácia", "pharmacy")
    ];

    public Task<Store?> Get(string storeId) => Task.FromResult(_stores.FirstOrDefault(s => s.Id == storeId));

    public Task<IList<Store>> List() => Task.FromResult<IList<Store>>(_stores);
}
