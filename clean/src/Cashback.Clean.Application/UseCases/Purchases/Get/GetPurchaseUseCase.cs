using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;

namespace Cashback.Clean.Application.UseCases.Purchases.Get;

// Depois de um timeout, a loja também pode perguntar "essa compra foi registrada?".
public interface IGetPurchaseUseCase
{
    Task<ResponseStorePurchaseJson> Execute(Guid purchaseId);
}

public class GetPurchaseUseCase(IPurchaseRepository purchaseRepository) : IGetPurchaseUseCase
{
    public async Task<ResponseStorePurchaseJson> Execute(Guid purchaseId)
    {
        var purchase = await purchaseRepository.Get(purchaseId) ?? throw new NotFoundException("Compra não encontrada.");

        return new ResponseStorePurchaseJson(purchase.Id, purchase.Category, purchase.Percentage, purchase.Cashback);
    }
}
