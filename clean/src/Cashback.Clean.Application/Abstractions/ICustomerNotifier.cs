namespace Cashback.Clean.Application.Abstractions;

public interface ICustomerNotifier
{
    Task Notify(string customerId, string message);
}
