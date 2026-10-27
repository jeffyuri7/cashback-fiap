using Cashback.Clean.Application.UseCases.Customers.GetBalance;
using Cashback.Clean.Application.UseCases.Customers.List;
using Cashback.Clean.Application.UseCases.Customers.ListPurchases;
using Cashback.Clean.Application.UseCases.Customers.Redeem;
using Cashback.Clean.Application.UseCases.Purchases.Get;
using Cashback.Clean.Application.UseCases.Purchases.Register;
using Cashback.Clean.Application.UseCases.Stores.List;
using Cashback.Clean.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Cashback.Clean.Application;

public static class DependencyInjectionExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        AddRules(services);
        AddUseCases(services);
    }

    // Regras de cashback (singleton: não guardam estado). Campanha nova = uma linha nova aqui.
    private static void AddRules(IServiceCollection services)
    {
        services.AddSingleton<ICashbackRule, CategoryRule>();
        services.AddSingleton<ICashbackRule, SupermarketDoubleCampaign>();
        services.AddSingleton<ICashbackRule, PharmacyCampaign>();
        services.AddSingleton<CashbackCalculator>();
    }

    // Casos de uso (scoped: um por requisição).
    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IRegisterPurchaseUseCase, RegisterPurchaseUseCase>();
        services.AddScoped<IGetPurchaseUseCase, GetPurchaseUseCase>();
        services.AddScoped<IGetBalanceUseCase, GetBalanceUseCase>();
        services.AddScoped<IRedeemCashbackUseCase, RedeemCashbackUseCase>();
        services.AddScoped<IListCustomersUseCase, ListCustomersUseCase>();
        services.AddScoped<IListPurchasesUseCase, ListPurchasesUseCase>();
        services.AddScoped<IListStoresUseCase, ListStoresUseCase>();
    }
}
