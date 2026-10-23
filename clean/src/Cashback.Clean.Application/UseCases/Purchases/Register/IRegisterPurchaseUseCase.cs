using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.Purchases.Register;

public interface IRegisterPurchaseUseCase
{
    /// <param name="idempotencyKey">Identificador da operação, gerado pela loja. Com ele, repetir não paga duas vezes.</param>
    Task<RegisterPurchaseResult> Execute(RequestRegisterPurchaseJson request, string? idempotencyKey = null);
}

/// <param name="Replayed">true quando a resposta é a guardada da primeira vez (a compra não foi registrada de novo).</param>
public record RegisterPurchaseResult(ResponseRegisterPurchaseJson Response, bool Replayed);
