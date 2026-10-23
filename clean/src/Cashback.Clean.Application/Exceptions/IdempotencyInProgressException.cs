namespace Cashback.Clean.Application.Exceptions;

/// <summary>A repetição chegou enquanto o pedido original ainda está sendo processado.</summary>
public class IdempotencyInProgressException()
    : Exception("A compra com esta Idempotency-Key ainda está sendo processada. Tente de novo em instantes.");
