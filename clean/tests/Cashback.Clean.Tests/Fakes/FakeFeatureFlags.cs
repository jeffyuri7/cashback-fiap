using Cashback.Clean.Application.Abstractions;

namespace Cashback.Clean.Tests.Fakes;

public class FakeFeatureFlags : IFeatureFlags
{
    public Dictionary<string, FeatureFlag> Flags { get; } = new();

    public Task<IList<FeatureFlag>> List() => Task.FromResult<IList<FeatureFlag>>(Flags.Values.ToList());

    public Task<FeatureFlag?> Get(string name) => Task.FromResult(Flags.GetValueOrDefault(name));

    public Task Save(FeatureFlag flag)
    {
        Flags[flag.Name] = flag;
        return Task.CompletedTask;
    }
}
