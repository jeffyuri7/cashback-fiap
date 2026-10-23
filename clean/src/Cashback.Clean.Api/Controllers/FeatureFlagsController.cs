using Cashback.Clean.Api.Models;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.UseCases.FeatureFlags.Change;
using Cashback.Clean.Application.UseCases.FeatureFlags.List;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers;

/// <summary>
/// Ligar e desligar funcionalidades em produção, sem deploy (aula 3): a aba Operação do painel usa estas rotas.
/// Num sistema real, só a operação teria acesso (autenticação e perfil); no curso, quem chama se identifica no header X-Operador.
/// </summary>
[ApiController]
[Route("feature-flags")]
public class FeatureFlagsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IList<ResponseFeatureFlagJson>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromServices] IListFeatureFlagsUseCase useCase) => Ok(await useCase.Execute());

    [HttpPut("{name}")]
    [ProducesResponseType(typeof(ResponseFeatureFlagJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Change(
        [FromServices] IChangeFeatureFlagUseCase useCase,
        [FromRoute] string name,
        [FromBody] RequestChangeFeatureFlagJson request,
        [FromHeader(Name = "X-Operador")] string? operador)
    {
        var response = await useCase.Execute(name, request, string.IsNullOrWhiteSpace(operador) ? "não identificado" : operador);

        return Ok(response);
    }
}
