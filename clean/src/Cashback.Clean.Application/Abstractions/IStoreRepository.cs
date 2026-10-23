using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.Abstractions;

public interface IStoreRepository
{
    Task<Store?> Get(string storeId);
    Task<IList<Store>> List();
}
