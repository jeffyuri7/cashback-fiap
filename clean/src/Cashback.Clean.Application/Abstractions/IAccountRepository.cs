using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.Abstractions;

public interface IAccountRepository
{
    Task<CashbackAccount?> Get(string customerId);
    Task<IList<CashbackAccount>> List();
    Task Save(CashbackAccount account);
}
