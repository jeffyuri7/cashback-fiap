using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.Customers.GetBalance;

public interface IGetBalanceUseCase
{
    Task<ResponseBalanceJson> Execute(string customerId);
}
