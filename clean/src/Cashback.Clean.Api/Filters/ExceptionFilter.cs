using Cashback.Clean.Api.Models;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Cashback.Clean.Api.Filters;

// Só aqui, na borda, as exceções viram códigos HTTP. O domínio e a aplicação não sabem o que é HTTP.
public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            ValidationException e => new BadRequestObjectResult(new ResponseErrorJson(e.Errors)),
            DomainException e => new BadRequestObjectResult(new ResponseErrorJson([e.Message])),
            NotFoundException => new NotFoundResult(),
            // as duas respostas que o rascunho do Idempotency-Key na IETF recomenda
            IdempotencyKeyReusedException e => new UnprocessableEntityObjectResult(new ResponseErrorJson([e.Message])),
            IdempotencyInProgressException e => new ConflictObjectResult(new ResponseErrorJson([e.Message])),
            _ => new ObjectResult(new ResponseErrorJson(["Erro desconhecido."]))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };

        context.ExceptionHandled = true;
    }
}
