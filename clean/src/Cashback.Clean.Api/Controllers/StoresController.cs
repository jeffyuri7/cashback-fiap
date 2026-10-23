using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.UseCases.Stores.List;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers;

[ApiController]
[Route("stores")]
public class StoresController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IList<ResponseStoreJson>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromServices] IListStoresUseCase useCase)
    {
        var response = await useCase.Execute();

        return Ok(response);
    }
}
