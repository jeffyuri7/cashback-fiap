using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.Customers.Redeem;

public interface IRedeemCashbackUseCase
{
    Task<ResponseRedeemJson> Execute(string customerId, RequestRedeemJson request);
}
