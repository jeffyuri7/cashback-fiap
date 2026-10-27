using System.Globalization;
using System.Text.Json;
using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;
using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Domain.Rules;

namespace Cashback.Clean.Application.UseCases.Purchases.Register;

public class RegisterPurchaseUseCase(
    CashbackCalculator calculator,
    IAccountRepository accountRepository,
    IStoreRepository storeRepository,
    IPurchaseRepository purchaseRepository,
    IIdempotencyStore idempotencyStore,
    ICustomerNotifier notifier,
    TimeProvider clock) : IRegisterPurchaseUseCase
{
    public async Task<RegisterPurchaseResult> Execute(RequestRegisterPurchaseJson request, string? idempotencyKey = null)
    {
        RegisterPurchaseValidator.Validate(request, idempotencyKey);

        if (idempotencyKey is null)
            return new RegisterPurchaseResult(await Register(request), Replayed: false);

        // "Não pagar a mesma compra duas vezes" é regra do app, não do HTTP: por isso mora aqui.
        // A chave vale por loja: duas lojas podem gerar o mesmo valor sem se atrapalhar.
        var scope = $"purchases:{request.StoreId}";
        var fingerprint = Fingerprint(request);

        var existing = await idempotencyStore.Reserve(scope, idempotencyKey, fingerprint);
        if (existing is not null)
        {
            if (existing.Fingerprint != fingerprint) throw new IdempotencyKeyReusedException();
            if (existing.Response is null) throw new IdempotencyInProgressException();
            return new RegisterPurchaseResult(JsonSerializer.Deserialize<ResponseRegisterPurchaseJson>(existing.Response)!, Replayed: true);
        }

        try
        {
            var response = await Register(request);
            await idempotencyStore.Complete(scope, idempotencyKey, JsonSerializer.Serialize(response));
            return new RegisterPurchaseResult(response, Replayed: false);
        }
        catch
        {
            // falhou antes de concluir: a chave volta a ficar livre para a loja tentar de novo
            await idempotencyStore.Release(scope, idempotencyKey);
            throw;
        }
    }

    private async Task<ResponseRegisterPurchaseJson> Register(RequestRegisterPurchaseJson request)
    {
        var store = await storeRepository.Get(request.StoreId!)
            ?? throw new ValidationException(["Loja parceira não encontrada."]);
        var account = await accountRepository.Get(request.CustomerId!)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var now = clock.GetUtcNow();
        var quote = calculator.Calculate(request.Amount, store.Category, now);
        var credited = account.Credit(quote.Amount, now);

        var purchase = new Purchase(Guid.NewGuid(), account.Owner.Id, store.Id, request.Amount, store.Category,
            quote.Percentage, credited, now);

        await accountRepository.Save(account);
        await purchaseRepository.Add(purchase);
        await notifier.Notify(account.Owner.Id, $"você ganhou R$ {credited:0.00} de cashback!");

        return new ResponseRegisterPurchaseJson(purchase.Id, store.Category, quote.Percentage, credited, account.Balance);
    }

    // O que identifica "a mesma compra": quem, onde e quanto.
    private static string Fingerprint(RequestRegisterPurchaseJson request) =>
        string.Join('|', request.CustomerId, request.StoreId, request.Amount.ToString("0.00", CultureInfo.InvariantCulture));
}
