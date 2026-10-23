using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Infrastructure.DataAccess;
using Dapper;

namespace Cashback.Clean.Infrastructure.Repositories;

/// <summary>
/// As flags ficam no banco da aplicação (aula 3). Hodgson prefere configuração versionada no código, mas um
/// kill switch precisa mudar em segundos, em produção, sem deploy: por isso a troca grava quem mudou e quando.
/// </summary>
public class FeatureFlagRepository(PostgresConnection db) : IFeatureFlags
{
    private const string Columns =
        "name, description, state, test_customers AS TestCustomers, starts_at AS StartsAt, ends_at AS EndsAt, " +
        "changed_by AS ChangedBy, changed_at AS ChangedAt";

    public async Task<IList<FeatureFlag>> List()
    {
        await using var conn = db.Open();
        var rows = await conn.QueryAsync<FlagRow>($"SELECT {Columns} FROM feature_flags ORDER BY name");
        return rows.Select(r => r.ToFlag()).ToList();
    }

    public async Task<FeatureFlag?> Get(string name)
    {
        await using var conn = db.Open();
        var row = await conn.QuerySingleOrDefaultAsync<FlagRow>($"SELECT {Columns} FROM feature_flags WHERE name = @name", new { name });
        return row?.ToFlag();
    }

    public async Task Save(FeatureFlag flag)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            "UPDATE feature_flags SET state = @State, changed_by = @ChangedBy, changed_at = @ChangedAt WHERE name = @Name",
            new { flag.Name, State = flag.State.ToString().ToLowerInvariant(), flag.ChangedBy, flag.ChangedAt });
    }

    // classe com propriedades (e não record posicional): o Dapper não casa o text[] do Postgres com um construtor
    private class FlagRow
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string State { get; init; } = "off";
        public string[] TestCustomers { get; init; } = [];
        public DateTime? StartsAt { get; init; }
        public DateTime? EndsAt { get; init; }
        public string? ChangedBy { get; init; }
        public DateTime? ChangedAt { get; init; }

        public FeatureFlag ToFlag() => new(Name, Description, Enum.Parse<FlagState>(State, ignoreCase: true), TestCustomers,
            Utc(StartsAt), Utc(EndsAt), ChangedBy, Utc(ChangedAt));

        private static DateTimeOffset? Utc(DateTime? value) =>
            value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
