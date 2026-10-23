namespace Cashback.Clean.Application.Dtos.Responses;

/// <summary>
/// A compra como a loja parceira vê na v2: só o que ela precisa para conciliar.
/// O saldo do cliente (que a v1 devolvia) não é da conta da loja.
/// </summary>
public record ResponseStorePurchaseJson(Guid PurchaseId, string Category, decimal Percentage, decimal Cashback)
{
    public static ResponseStorePurchaseJson From(ResponseRegisterPurchaseJson registered) =>
        new(registered.PurchaseId, registered.Category, registered.Percentage, registered.Cashback);
}
