namespace Cashback.Clean.Application.Abstractions;

/// <summary>Onde as feature flags ficam guardadas (aula 3). A aplicação não sabe se é um banco, um arquivo ou um serviço.</summary>
public interface IFeatureFlags
{
    Task<IList<FeatureFlag>> List();
    Task<FeatureFlag?> Get(string name);
    Task Save(FeatureFlag flag);
}

/// <summary>Desligada · só o grupo de teste (os funcionários) · todos, dentro do horário.</summary>
public enum FlagState { Off, Test, On }

/// <summary>
/// Uma feature flag com horário. As três situações:
/// desligada (ninguém vê), grupo de teste (só os clientes da lista, a qualquer hora, para testar em produção antes)
/// e todos (todo mundo, mas só entre StartsAt e EndsAt: a campanha liga e desliga sozinha, sem deploy de madrugada).
/// </summary>
public record FeatureFlag(
    string Name, string Description, FlagState State, string[] TestCustomers,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string? ChangedBy, DateTimeOffset? ChangedAt)
{
    // O "roteador" da flag (Hodgson): decide se ela vale para este cliente, nesta hora.
    public bool IsEnabledFor(string customerId, DateTimeOffset now) => State switch
    {
        FlagState.Test => TestCustomers.Contains(customerId),
        FlagState.On => InWindow(now),
        _ => false
    };

    public bool InWindow(DateTimeOffset now) => (StartsAt is null || now >= StartsAt) && (EndsAt is null || now < EndsAt);
}
