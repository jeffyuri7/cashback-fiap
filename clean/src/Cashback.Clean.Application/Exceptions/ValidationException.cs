namespace Cashback.Clean.Application.Exceptions;

/// <summary>A requisição chegou com dados inválidos. Carrega todos os erros de uma vez.</summary>
public class ValidationException(IList<string> errors) : Exception(string.Join(" ", errors))
{
    public IList<string> Errors { get; } = errors;
}
