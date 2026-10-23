using Cashback.Clean.Application.Abstractions;

namespace Cashback.Clean.Tests.Fakes;

public class FakeNotifier : ICustomerNotifier
{
    public List<string> Messages { get; } = new();

    public Task Notify(string customerId, string message)
    {
        Messages.Add($"{customerId}: {message}");
        return Task.CompletedTask;
    }
}
