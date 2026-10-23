using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Application.UseCases.Purchases.Get;
using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Tests.Fakes;

namespace Cashback.Clean.Tests.UseCases;

public class GetPurchaseUseCaseTests
{
    private readonly FakePurchaseRepository _purchases = new();

    [Fact]
    public async Task The_store_sees_the_purchase_without_the_customer_balance()
    {
        var purchase = new Purchase(Guid.NewGuid(), "c-001", "s-mercado", 250m, "supermarket", 0.04m, 10m, DateTimeOffset.UnixEpoch);
        await _purchases.Add(purchase);

        var response = await new GetPurchaseUseCase(_purchases).Execute(purchase.Id);

        Assert.Equal(purchase.Id, response.PurchaseId);
        Assert.Equal(10m, response.Cashback);
    }

    [Fact]
    public async Task Unknown_purchase_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new GetPurchaseUseCase(_purchases).Execute(Guid.NewGuid()));
    }
}
