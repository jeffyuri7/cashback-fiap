using Cashback.Clean.Api.Models;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.UseCases.Customers.GetBalance;
using Cashback.Clean.Application.UseCases.Customers.List;
using Cashback.Clean.Application.UseCases.Customers.ListPurchases;
using Cashback.Clean.Application.UseCases.Customers.Redeem;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IList<ResponseCustomerJson>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromServices] IListCustomersUseCase useCase)
    {
        var response = await useCase.Execute();

        return Ok(response);
    }

    [HttpGet("{id}/balance")]
    [ProducesResponseType(typeof(ResponseBalanceJson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(
        [FromServices] IGetBalanceUseCase useCase,
        [FromRoute] string id)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }

    [HttpGet("{id}/purchases")]
    [ProducesResponseType(typeof(IList<ResponsePurchaseJson>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListPurchases(
        [FromServices] IListPurchasesUseCase useCase,
        [FromRoute] string id)
    {
        var response = await useCase.Execute(id);

        return Ok(response);
    }

    [HttpPost("{id}/redemptions")]
    [ProducesResponseType(typeof(ResponseRedeemJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Redeem(
        [FromServices] IRedeemCashbackUseCase useCase,
        [FromRoute] string id,
        [FromBody] RequestRedeemJson request)
    {
        var response = await useCase.Execute(id, request);

        return Ok(response);
    }
}
