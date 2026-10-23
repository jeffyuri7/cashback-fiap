using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.FeatureFlags.List;

public interface IListFeatureFlagsUseCase
{
    Task<IList<ResponseFeatureFlagJson>> Execute();
}

public class ListFeatureFlagsUseCase(IFeatureFlags featureFlags, TimeProvider clock) : IListFeatureFlagsUseCase
{
    public async Task<IList<ResponseFeatureFlagJson>> Execute()
    {
        var now = clock.GetUtcNow();
        return (await featureFlags.List()).Select(f => FeatureFlagJson.From(f, now)).ToList();
    }
}
