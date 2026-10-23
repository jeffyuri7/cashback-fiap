using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.FeatureFlags;

internal static class FeatureFlagJson
{
    public static readonly Dictionary<string, FlagState> States = new()
    {
        ["off"] = FlagState.Off, ["test"] = FlagState.Test, ["on"] = FlagState.On
    };

    public static ResponseFeatureFlagJson From(FeatureFlag flag, DateTimeOffset now) => new(
        flag.Name, flag.Description, States.First(s => s.Value == flag.State).Key, flag.TestCustomers,
        flag.StartsAt, flag.EndsAt, AudienceNow(flag, now), flag.ChangedBy, flag.ChangedAt);

    private static string AudienceNow(FeatureFlag flag, DateTimeOffset now) => flag.State switch
    {
        FlagState.Test => "test-group",
        FlagState.On when flag.InWindow(now) => "everyone",
        _ => "none"
    };
}
