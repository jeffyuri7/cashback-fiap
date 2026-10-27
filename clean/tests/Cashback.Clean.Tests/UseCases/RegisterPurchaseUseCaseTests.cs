using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Application.UseCases.Purchases.Register;
using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Domain.Rules;
using Cashback.Clean.Tests.Fakes;

namespace Cashback.Clean.Tests.UseCases;

// O caso de uso inteiro testado sem banco, sem HTTP e sem push: só com implementações falsas.
public class RegisterPurchaseUseCaseTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakePurchaseRepository _purchases = new();
    private readonly FakeIdempotencyStore _idempotency = new();
    private readonly FakeNotifier _notifier = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));

    public RegisterPurchaseUseCaseTests() =>
        _accounts.Saved["c-001"] = new CashbackAccount(new Customer("c-001", "Ana", "ana@email.com"), 0, 0, "2026-10");

    private RegisterPurchaseUseCase CreateUseCase() => new(
        new CashbackCalculator([new CategoryRule(), new SupermarketDoubleCampaign()]),
        _accounts, new FakeStoreRepository(), _purchases, _idempotency, _notifier, _clock);

    [Fact]
    public async Task Registers_the_purchase_credits_the_cashback_and_notifies()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 250m };

        var response = (await CreateUseCase().Execute(request)).Response;

        Assert.Equal(0.04m, response.Percentage);
        Assert.Equal(10m, response.Cashback);
        Assert.Equal(10m, response.Balance);
        Assert.Single(_purchases.Added);
        Assert.Equal(10m, _accounts.Saved["c-001"].Balance);
        Assert.Single(_notifier.Messages);
    }

    [Fact]
    public async Task Invalid_request_is_refused_with_all_errors()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "", StoreId = null, Amount = 0m };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => CreateUseCase().Execute(request));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Empty(_purchases.Added);
    }

    [Fact]
    public async Task Unknown_customer_is_not_found()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "c-999", StoreId = "s-mercado", Amount = 10m };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateUseCase().Execute(request));
    }

    // ───────────── aula 2: idempotência ─────────────

    [Fact]
    public async Task Retry_with_the_same_key_returns_the_first_response_and_does_not_pay_twice()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 250m };
        var useCase = CreateUseCase();

        var first = await useCase.Execute(request, "chave-1");
        var retry = await useCase.Execute(request, "chave-1");

        Assert.False(first.Replayed);
        Assert.True(retry.Replayed);
        Assert.Equal(first.Response, retry.Response);
        Assert.Single(_purchases.Added);
        Assert.Equal(10m, _accounts.Saved["c-001"].Balance);
    }

    [Fact]
    public async Task Retry_without_a_key_pays_twice()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 250m };
        var useCase = CreateUseCase();

        await useCase.Execute(request);
        await useCase.Execute(request);

        Assert.Equal(2, _purchases.Added.Count);
        Assert.Equal(20m, _accounts.Saved["c-001"].Balance);
    }

    [Fact]
    public async Task Same_key_with_another_purchase_is_refused()
    {
        var useCase = CreateUseCase();
        await useCase.Execute(new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 250m }, "chave-1");

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => useCase.Execute(
            new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 99m }, "chave-1"));
        Assert.Single(_purchases.Added);
    }

    [Fact]
    public async Task Retry_while_the_first_is_still_running_is_a_conflict()
    {
        var request = new RequestRegisterPurchaseJson { CustomerId = "c-001", StoreId = "s-mercado", Amount = 250m };
        await _idempotency.Reserve("purchases:s-mercado", "chave-1", "c-001|s-mercado|250.00");

        await Assert.ThrowsAsync<IdempotencyInProgressException>(() => CreateUseCase().Execute(request, "chave-1"));
    }

    [Fact]
    public async Task A_failed_attempt_frees_the_key_for_a_new_try()
    {
        var useCase = CreateUseCase();
        var unknownCustomer = new RequestRegisterPurchaseJson { CustomerId = "c-999", StoreId = "s-mercado", Amount = 10m };

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.Execute(unknownCustomer, "chave-1"));

        Assert.Empty(_idempotency.Records);
    }
}
