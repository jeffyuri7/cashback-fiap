using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.UseCases.Customers.Redeem;

public class RedeemCashbackUseCase(
    IAccountRepository accountRepository,
    IRedemptionRepository redemptionRepository,
    ICustomerNotifier notifier,
    TimeProvider clock) : IRedeemCashbackUseCase
{
    public async Task<ResponseRedeemJson> Execute(string customerId, RequestRedeemJson request)
    {
        var account = await accountRepository.Get(customerId)
            ?? throw new NotFoundException("Cliente não encontrado.");

        account.Redeem(request.Amount);

        var pixKey = account.Owner.PixKey;
        await accountRepository.Save(account);
        await redemptionRepository.Add(new Redemption(Guid.NewGuid(), customerId, request.Amount, pixKey, clock.GetUtcNow()));
        await notifier.Notify(customerId, $"resgate de R$ {request.Amount:0.00} enviado via PIX para {pixKey}.");

        return new ResponseRedeemJson(account.Balance, pixKey);
    }
}
