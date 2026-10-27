using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Infrastructure.Clock;
using Cashback.Clean.Infrastructure.DataAccess;
using Cashback.Clean.Infrastructure.Notification;
using Cashback.Clean.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cashback.Clean.Infrastructure;

public static class DependencyInjectionExtension
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddClock(services, configuration);

        // singleton: só guarda a connection string; cada repositório abre a própria conexão
        services.AddSingleton(new PostgresConnection(configuration.GetConnectionString("Cashback")!));

        // scoped: um por requisição
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IRedemptionRepository, RedemptionRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        // transient: um novo a cada uso
        services.AddTransient<ICustomerNotifier, LogCustomerNotifier>();
    }

    // O relógio do sistema, ou o relógio ajustável da demonstração (aula 3), se a configuração apontar o arquivo dele.
    private static void AddClock(IServiceCollection services, IConfiguration configuration)
    {
        var clockFile = configuration["Demo:ClockFile"];
        if (string.IsNullOrWhiteSpace(clockFile))
        {
            services.AddSingleton(TimeProvider.System);
            return;
        }

        var clock = new AdjustableClock(clockFile);
        services.AddSingleton(clock);
        services.AddSingleton<TimeProvider>(clock);
    }
}
