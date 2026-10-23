using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Infrastructure.DataAccess;
using Dapper;

namespace Cashback.Clean.Infrastructure.Repositories;

// As chaves valem 24 horas (política publicada no contrato). Depois disso, a mesma chave conta como nova.
public class IdempotencyStore(PostgresConnection db) : IIdempotencyStore
{
    public async Task<IdempotencyRecord?> Reserve(string scope, string key, string fingerprint)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            "DELETE FROM idempotency_keys WHERE scope = @scope AND key = @key AND created_at < now() - interval '24 hours'",
            new { scope, key });

        // A chave primária (scope, key) garante que, mesmo com duas repetições ao mesmo tempo, só uma reserva.
        var reserved = await conn.ExecuteAsync(
            """
            INSERT INTO idempotency_keys (scope, key, fingerprint, created_at) VALUES (@scope, @key, @fingerprint, now())
            ON CONFLICT (scope, key) DO NOTHING
            """, new { scope, key, fingerprint });
        if (reserved == 1) return null;

        return await conn.QuerySingleAsync<IdempotencyRecord>(
            "SELECT fingerprint, response::text AS response FROM idempotency_keys WHERE scope = @scope AND key = @key",
            new { scope, key });
    }

    public async Task Complete(string scope, string key, string response)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            "UPDATE idempotency_keys SET response = @response::jsonb WHERE scope = @scope AND key = @key",
            new { scope, key, response });
    }

    public async Task Release(string scope, string key)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync("DELETE FROM idempotency_keys WHERE scope = @scope AND key = @key", new { scope, key });
    }
}
