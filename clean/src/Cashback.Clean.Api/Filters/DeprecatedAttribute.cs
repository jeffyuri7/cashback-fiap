using Microsoft.AspNetCore.Mvc.Filters;

namespace Cashback.Clean.Api.Filters;

/// <summary>
/// Marca uma versão do contrato como depreciada. Toda resposta passa a avisar:
///   Deprecation (RFC 9745): desde quando está depreciada;
///   Sunset (RFC 8594): quando deixa de responder (nunca antes do Deprecation);
///   Link rel="successor-version" (RFC 5829): para onde migrar.
/// </summary>
public class DeprecatedAttribute(string since, string sunset, string successor) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers["Deprecation"] = $"@{DateTimeOffset.Parse(since).ToUnixTimeSeconds()}";
        headers["Sunset"] = DateTimeOffset.Parse(sunset).ToString("r");
        headers.Append("Link", $"<{successor}>; rel=\"successor-version\"");
    }
}
