using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Tests.Fakes;

// Uma implementação falsa da porta: o teste roda sem banco nenhum.
public class FakeAccountRepository : IAccountRepository
{
    public Dictionary<string, CashbackAccount> Saved { get; } = new();

    public Task<CashbackAccount?> Get(string customerId) => Task.FromResult(Saved.GetValueOrDefault(customerId));

    public Task<IList<CashbackAccount>> List() => Task.FromResult<IList<CashbackAccount>>(Saved.Values.ToList());

    public Task Save(CashbackAccount account)
    {
        Saved[account.Owner.Id] = account;
        return Task.CompletedTask;
    }
}
