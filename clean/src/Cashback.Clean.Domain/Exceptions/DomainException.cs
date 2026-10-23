namespace Cashback.Clean.Domain.Exceptions;

/// <summary>Uma regra de negócio foi violada (ex.: resgate maior que o saldo).</summary>
public class DomainException(string message) : Exception(message);
