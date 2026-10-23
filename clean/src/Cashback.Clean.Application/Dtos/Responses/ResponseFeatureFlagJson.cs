namespace Cashback.Clean.Application.Dtos.Responses;

/// <param name="State">off · test · on</param>
/// <param name="AudienceNow">para quem a flag vale agora: none · test-group · everyone</param>
public record ResponseFeatureFlagJson(
    string Name, string Description, string State, string[] TestCustomers,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string AudienceNow, string? ChangedBy, DateTimeOffset? ChangedAt);
