using Cashback.Clean.Api.Models;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Application.UseCases.Purchases.Get;
using Cashback.Clean.Application.UseCases.Purchases.Register;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers.V2;

// A v2 do contrato das lojas. O caso de uso é o MESMO da v1: a versão é assunto da borda (adaptador de interface).
// O que muda em relação à v1 (todas são breaking, por isso a versão nova):
//   1. Idempotency-Key obrigatório;
//   2. 201 Created + Location, em vez de 200 OK;
//   3. a resposta não traz mais o saldo do cliente.
[ApiController]
[Route("v2/purchases")]
public class PurchasesV2Controller : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResponseStorePurchaseJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromServices] IRegisterPurchaseUseCase useCase,
        [FromBody] RequestRegisterPurchaseJson request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ValidationException(["O header Idempotency-Key é obrigatório na v2 (um UUID por compra; repita o mesmo no retry)."]);

        var result = await useCase.Execute(request, IdempotencyKeyHeader.Parse(idempotencyKey));

        if (result.Replayed) Response.Headers["Idempotent-Replayed"] = "true";
        var purchase = ResponseStorePurchaseJson.From(result.Response);
        return CreatedAtAction(nameof(Get), new { id = purchase.PurchaseId }, purchase);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseStorePurchaseJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromServices] IGetPurchaseUseCase useCase, [FromRoute] Guid id)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }
}
