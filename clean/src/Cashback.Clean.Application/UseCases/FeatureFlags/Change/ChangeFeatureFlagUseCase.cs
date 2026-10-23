using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Requests;
using Cashback.Clean.Application.Dtos.Responses;
using Cashback.Clean.Application.Exceptions;

namespace Cashback.Clean.Application.UseCases.FeatureFlags.Change;

public interface IChangeFeatureFlagUseCase
{
    Task<ResponseFeatureFlagJson> Execute(string name, RequestChangeFeatureFlagJson request, string changedBy);
}

/// <summary>Liga ou desliga uma flag em produção, sem deploy. Guarda quem mudou e quando: a flag é dinâmica, mas não anônima.</summary>
public class ChangeFeatureFlagUseCase(IFeatureFlags featureFlags, TimeProvider clock) : IChangeFeatureFlagUseCase
{
    public async Task<ResponseFeatureFlagJson> Execute(string name, RequestChangeFeatureFlagJson request, string changedBy)
    {
        if (request.State is null || !FeatureFlagJson.States.TryGetValue(request.State, out var state))
            throw new ValidationException(["O estado da flag deve ser off, test ou on."]);

        var flag = await featureFlags.Get(name) ?? throw new NotFoundException("Flag não encontrada.");

        var now = clock.GetUtcNow();
        var changed = flag with { State = state, ChangedBy = changedBy, ChangedAt = now };
        await featureFlags.Save(changed);

        return FeatureFlagJson.From(changed, now);
    }
}
