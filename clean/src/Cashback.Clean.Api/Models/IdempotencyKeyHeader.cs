namespace Cashback.Clean.Api.Models;

public static class IdempotencyKeyHeader
{
    // O rascunho da IETF define o valor como string entre aspas ("abc"); muitas APIs aceitam sem aspas. Aceitamos os dois.
    public static string? Parse(string? header) =>
        header is { Length: >= 2 } && header.StartsWith('"') && header.EndsWith('"') ? header[1..^1] : header;
}
