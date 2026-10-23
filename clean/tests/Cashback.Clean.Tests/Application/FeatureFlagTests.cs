using Cashback.Clean.Application.Abstractions;

namespace Cashback.Clean.Tests.Application;

// Para quem a flag vale, e quando (aula 3).
public class FeatureFlagTests
{
    private static readonly DateTimeOffset Starts = new(2026, 11, 27, 0, 0, 0, TimeSpan.FromHours(-3));
    private static readonly DateTimeOffset Ends = new(2026, 11, 30, 0, 0, 0, TimeSpan.FromHours(-3));

    private static FeatureFlag Flag(FlagState state) =>
        new("black-friday", "Black Friday", state, ["c-001"], Starts, Ends, null, null);

    [Fact]
    public void Off_is_off_for_everyone()
    {
        Assert.False(Flag(FlagState.Off).IsEnabledFor("c-001", Starts.AddHours(1)));
    }

    [Fact]
    public void Test_is_on_only_for_the_test_group_even_before_the_start()
    {
        var thursday = Starts.AddHours(-7);

        Assert.True(Flag(FlagState.Test).IsEnabledFor("c-001", thursday));
        Assert.False(Flag(FlagState.Test).IsEnabledFor("c-002", thursday));
    }

    [Fact]
    public void On_turns_itself_on_at_the_start_and_off_at_the_end()
    {
        var flag = Flag(FlagState.On);

        Assert.False(flag.IsEnabledFor("c-002", Starts.AddSeconds(-1)));
        Assert.True(flag.IsEnabledFor("c-002", Starts));
        Assert.True(flag.IsEnabledFor("c-002", Ends.AddSeconds(-1)));
        Assert.False(flag.IsEnabledFor("c-002", Ends));
    }
}
