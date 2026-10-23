using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Domain.Exceptions;

namespace Cashback.Clean.Tests.Domain;

public class CashbackAccountTests
{
    private static readonly DateTimeOffset October = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset November = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static CashbackAccount NewAccount() => new(new Customer("c-001", "Ana", "ana@email.com"), 0, 0, "2026-10");

    [Fact]
    public void Credit_respects_the_monthly_limit()
    {
        var account = NewAccount();
        account.Credit(90m, October);

        var credited = account.Credit(30m, October);

        Assert.Equal(10m, credited);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void A_new_month_starts_a_new_limit()
    {
        var account = NewAccount();
        account.Credit(100m, October);

        var credited = account.Credit(30m, November);

        Assert.Equal(30m, credited);
        Assert.Equal(130m, account.Balance);
    }

    [Fact]
    public void Redeem_more_than_the_balance_is_refused()
    {
        var account = NewAccount();
        account.Credit(10m, October);

        var exception = Assert.Throws<DomainException>(() => account.Redeem(50m));

        Assert.Equal("Saldo insuficiente.", exception.Message);
        Assert.Equal(10m, account.Balance);
    }
}
