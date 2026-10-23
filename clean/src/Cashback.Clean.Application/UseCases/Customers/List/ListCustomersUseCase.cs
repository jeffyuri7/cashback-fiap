using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.Customers.List;

public interface IListCustomersUseCase
{
    Task<IList<ResponseCustomerJson>> Execute();
}

public class ListCustomersUseCase(IAccountRepository accountRepository) : IListCustomersUseCase
{
    public async Task<IList<ResponseCustomerJson>> Execute() =>
        (await accountRepository.List()).Select(a => new ResponseCustomerJson(a.Owner.Id, a.Owner.Name, a.Balance)).ToList();
}
