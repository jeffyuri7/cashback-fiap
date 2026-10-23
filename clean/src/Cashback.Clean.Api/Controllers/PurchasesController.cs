using Cashback.Clean.Api.Filters;
using Cashback.Clean.Api.Models;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.UseCases.Purchases.Register;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers;

// v1: depreciada desde 01/11/2026, sai do ar em 31/03/2027. As lojas migram para a v2 nesse prazo.
[ApiController]
[Route("purchases")]
[Deprecated(since: "2026-11-01T00:00:00Z", sunset: "2027-03-31T23:59:59Z", successor: "/v2/purchases")]
public class PurchasesController : ControllerBase
{
    // Idempotency-Key OPCIONAL: as lojas que já integram e não mandam a chave continuam funcionando.
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisterPurchaseJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromServices] IRegisterPurchaseUseCase useCase,
        [FromBody] RequestRegisterPurchaseJson request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var result = await useCase.Execute(request, IdempotencyKeyHeader.Parse(idempotencyKey));

        if (result.Replayed) Response.Headers["Idempotent-Replayed"] = "true";
        return Ok(result.Response);
    }
}
