using Cashback.Clean.Domain.Exceptions;

namespace Cashback.Clean.Domain.Entities;

/// <summary>
/// A conta de cashback protege as próprias regras: o saldo só muda por Credit e Redeem,
/// nunca fica negativo e o crédito do mês respeita o teto.
/// </summary>
public class CashbackAccount
{
    public const decimal MonthlyLimit = 100m;

    public Customer Owner { get; }
    public decimal Balance { get; private set; }
    public decimal CreditedThisMonth { get; private set; }
    public string MonthRef { get; private set; }

    public CashbackAccount(Customer owner, decimal balance, decimal creditedThisMonth, string monthRef)
    {
        Owner = owner;
        Balance = balance;
        CreditedThisMonth = creditedThisMonth;
        MonthRef = monthRef;
    }

    /// <summary>Credita respeitando o teto mensal. Devolve quanto foi de fato creditado.</summary>
    public decimal Credit(decimal amount, DateTimeOffset now)
    {
        if (amount < 0) throw new DomainException("O crédito não pode ser negativo.");

        StartNewMonthIfNeeded(now);

        var credited = Math.Min(amount, MonthlyLimit - CreditedThisMonth);
        Balance += credited;
        CreditedThisMonth += credited;
        return credited;
    }

    public void Redeem(decimal amount)
    {
        if (amount <= 0) throw new DomainException("O valor do resgate deve ser maior que zero.");
        if (amount > Balance) throw new DomainException("Saldo insuficiente.");

        Balance -= amount;
    }

    public decimal CreditedIn(DateTimeOffset now) => MonthRef == ToMonthRef(now) ? CreditedThisMonth : 0;

    private void StartNewMonthIfNeeded(DateTimeOffset now)
    {
        var month = ToMonthRef(now);
        if (month == MonthRef) return;

        MonthRef = month;
        CreditedThisMonth = 0;
    }

    public static string ToMonthRef(DateTimeOffset date) => date.ToString("yyyy-MM");
}
