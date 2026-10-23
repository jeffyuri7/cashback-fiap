namespace Cashback.Clean.Application.Exceptions;

/// <summary>A mesma chave de idempotência chegou com um pedido diferente do original.</summary>
public class IdempotencyKeyReusedException()
    : Exception("Esta Idempotency-Key já foi usada com outra compra. Gere uma chave nova para cada compra.");
