namespace Cashback.Clean.Api.Models;

/// <summary>O formato de erro do contrato da API.</summary>
public record ResponseErrorJson(IList<string> Errors);
