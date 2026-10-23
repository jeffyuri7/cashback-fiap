using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Exceptions;

namespace Cashback.Clean.Application.UseCases.Purchases.Register;

// Validação de formato: fica na entrada do caso de uso, não no domínio.
public static class RegisterPurchaseValidator
{
    public static void Validate(RequestRegisterPurchaseJson request, string? idempotencyKey = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CustomerId)) errors.Add("O cliente é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.StoreId)) errors.Add("A loja é obrigatória.");
        if (request.Amount <= 0) errors.Add("O valor da compra deve ser maior que zero.");
        if (idempotencyKey is not null && (idempotencyKey.Trim().Length == 0 || idempotencyKey.Length > 255))
            errors.Add("A Idempotency-Key deve ter de 1 a 255 caracteres.");
        if (errors.Count > 0) throw new ValidationException(errors);
    }
}
