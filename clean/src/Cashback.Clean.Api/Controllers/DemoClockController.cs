using Cashback.Clean.Api.Models;
using Cashback.Clean.Infrastructure.Clock;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Controllers;

/// <summary>
/// Só para a demonstração em sala (aula 3): ver e ajustar a hora do app, para "pular" para a Black Friday.
/// Fora da demonstração (sem Demo:ClockFile), responde 404: não existe relógio ajustável.
/// </summary>
[ApiController]
[Route("demo/clock")]
public class DemoClockController(TimeProvider clock) : ControllerBase
{
    private AdjustableClock? Adjustable => HttpContext.RequestServices.GetService<AdjustableClock>();

    [HttpGet]
    public IActionResult Get() => Ok(new ResponseClockJson(clock.GetUtcNow(), Adjustable?.IsAdjusted ?? false));

    [HttpPut]
    public IActionResult Set([FromBody] RequestClockJson request)
    {
        if (Adjustable is null) return NotFound();
        if (request.Now is null) return BadRequest(new ResponseErrorJson(["Informe a hora no campo now (ex.: 2026-11-27T00:00:00-03:00)."]));

        Adjustable.Set(request.Now.Value);
        return Ok(new ResponseClockJson(clock.GetUtcNow(), true));
    }

    [HttpDelete]
    public IActionResult Reset()
    {
        if (Adjustable is null) return NotFound();

        Adjustable.Reset();
        return Ok(new ResponseClockJson(clock.GetUtcNow(), false));
    }

    public record RequestClockJson(DateTimeOffset? Now);

    public record ResponseClockJson(DateTimeOffset Now, bool Adjusted);
}
