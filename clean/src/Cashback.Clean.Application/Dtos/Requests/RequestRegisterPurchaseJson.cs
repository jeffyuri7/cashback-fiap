namespace Cashback.Clean.Application.Dtos.Requests;

public class RequestRegisterPurchaseJson
{
    public string? CustomerId { get; set; }
    public string? StoreId { get; set; }
    public decimal Amount { get; set; }
}
