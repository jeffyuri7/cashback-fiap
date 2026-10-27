using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;

namespace Cashback.Clean.Application.UseCases.Customers.ListPurchases;

public interface IListPurchasesUseCase
{
    Task<IList<ResponsePurchaseJson>> Execute(string customerId);
}

public class ListPurchasesUseCase(IAccountRepository accountRepository, IPurchaseRepository purchaseRepository)
    : IListPurchasesUseCase
{
    public async Task<IList<ResponsePurchaseJson>> Execute(string customerId)
    {
        _ = await accountRepository.Get(customerId) ?? throw new NotFoundException("Cliente não encontrado.");

        var purchases = await purchaseRepository.ListByCustomer(customerId);
        return purchases
            .Select(p => new ResponsePurchaseJson(p.Id, p.StoreName, p.Amount, p.Percentage, p.Cashback, p.CreatedAt))
            .ToList();
    }
}
