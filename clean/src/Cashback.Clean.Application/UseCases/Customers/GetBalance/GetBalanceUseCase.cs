using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.UseCases.Customers.GetBalance;

public class GetBalanceUseCase(IAccountRepository accountRepository, TimeProvider clock) : IGetBalanceUseCase
{
    public async Task<ResponseBalanceJson> Execute(string customerId)
    {
        var account = await accountRepository.Get(customerId)
            ?? throw new NotFoundException("Cliente não encontrado.");

        return new ResponseBalanceJson(
            account.Owner.Id,
            account.Owner.Name,
            account.Balance,
            account.CreditedIn(clock.GetUtcNow()),
            CashbackAccount.MonthlyLimit);
    }
}
