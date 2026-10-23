using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Infrastructure.DataAccess;
using Dapper;

namespace Cashback.Clean.Infrastructure.Repositories;

public class AccountRepository(PostgresConnection db) : IAccountRepository
{
    private const string Select =
        """
        SELECT c.id, c.name, c.pix_key AS PixKey, a.balance, a.credited_this_month AS CreditedThisMonth,
               a.month_ref AS MonthRef
        FROM customers c JOIN cashback_accounts a ON a.customer_id = c.id
        """;

    public async Task<CashbackAccount?> Get(string customerId)
    {
        await using var conn = db.Open();
        var row = await conn.QuerySingleOrDefaultAsync<AccountRow>(Select + " WHERE c.id = @customerId", new { customerId });
        return row?.ToEntity();
    }

    public async Task<IList<CashbackAccount>> List()
    {
        await using var conn = db.Open();
        var rows = await conn.QueryAsync<AccountRow>(Select + " ORDER BY c.name");
        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task Save(CashbackAccount account)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            "UPDATE cashback_accounts SET balance = @Balance, credited_this_month = @CreditedThisMonth, month_ref = @MonthRef WHERE customer_id = @Id",
            new { account.Balance, account.CreditedThisMonth, account.MonthRef, account.Owner.Id });
    }

    // O formato da tabela fica aqui: a entidade não conhece colunas.
    private record AccountRow(string Id, string Name, string PixKey, decimal Balance, decimal CreditedThisMonth, string MonthRef)
    {
        public CashbackAccount ToEntity() => new(new Customer(Id, Name, PixKey), Balance, CreditedThisMonth, MonthRef);
    }
}
