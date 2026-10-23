using Cashback.Clean.Application.Abstractions;

namespace Cashback.Clean.Tests.Fakes;

public class FakeIdempotencyStore : IIdempotencyStore
{
    public Dictionary<(string Scope, string Key), IdempotencyRecord> Records { get; } = new();

    public Task<IdempotencyRecord?> Reserve(string scope, string key, string fingerprint)
    {
        if (Records.TryGetValue((scope, key), out var existing)) return Task.FromResult<IdempotencyRecord?>(existing);
        Records[(scope, key)] = new IdempotencyRecord(fingerprint, null);
        return Task.FromResult<IdempotencyRecord?>(null);
    }

    public Task Complete(string scope, string key, string response)
    {
        Records[(scope, key)] = Records[(scope, key)] with { Response = response };
        return Task.CompletedTask;
    }

    public Task Release(string scope, string key)
    {
        Records.Remove((scope, key));
        return Task.CompletedTask;
    }
}
