using Cashback.Clean.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cashback.Clean.Infrastructure.Notification;

/// <summary>Simula o push no log.</summary>
public class LogCustomerNotifier(ILogger<LogCustomerNotifier> logger) : ICustomerNotifier
{
    public Task Notify(string customerId, string message)
    {
        logger.LogInformation("[PUSH] {CustomerId}: {Message}", customerId, message);
        return Task.CompletedTask;
    }
}
