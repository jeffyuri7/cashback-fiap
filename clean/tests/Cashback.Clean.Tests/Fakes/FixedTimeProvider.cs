namespace Cashback.Clean.Tests.Fakes;

/// <summary>Um relógio parado numa data escolhida pelo teste.</summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
