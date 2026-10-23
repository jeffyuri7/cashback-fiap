namespace Cashback.Clean.Application.Abstractions;

/// <summary>
/// Lembra o resultado de cada operação identificada por uma chave de idempotência,
/// para que a repetição (retry) devolva o mesmo resultado em vez de executar de novo.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Reserva a chave antes de executar. Devolve null se a chave é nova (e fica reservada agora)
    /// ou o registro que já existia para ela.
    /// </summary>
    Task<IdempotencyRecord?> Reserve(string scope, string key, string fingerprint);

    /// <summary>Guarda a resposta da operação concluída.</summary>
    Task Complete(string scope, string key, string response);

    /// <summary>Libera a chave quando a operação falhou, para o cliente poder tentar de novo.</summary>
    Task Release(string scope, string key);
}

/// <param name="Fingerprint">Resumo do pedido original: a mesma chave não pode vir com outro pedido.</param>
/// <param name="Response">A resposta guardada, ou null enquanto a operação ainda está em andamento.</param>
public record IdempotencyRecord(string Fingerprint, string? Response);
