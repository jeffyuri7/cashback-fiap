using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Domain.Entities;
using Cashback.Clean.Infrastructure.DataAccess;
using Dapper;

namespace Cashback.Clean.Infrastructure.Repositories;

public class StoreRepository(PostgresConnection db) : IStoreRepository
{
    public async Task<Store?> Get(string storeId)
    {
        await using var conn = db.Open();
        return await conn.QuerySingleOrDefaultAsync<Store>("SELECT id, name, category FROM stores WHERE id = @storeId", new { storeId });
    }

    public async Task<IList<Store>> List()
    {
        await using var conn = db.Open();
        return (await conn.QueryAsync<Store>("SELECT id, name, category FROM stores ORDER BY name")).ToList();
    }
}

public class PurchaseRepository(PostgresConnection db) : IPurchaseRepository
{
    public async Task Add(Purchase purchase)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            """
            INSERT INTO purchases (id, customer_id, store_id, amount, category, percentage, cashback, created_at)
            VALUES (@Id, @CustomerId, @StoreId, @Amount, @Category, @Percentage, @Cashback, @CreatedAt)
            """, purchase);
    }

    public async Task<Purchase?> Get(Guid id)
    {
        await using var conn = db.Open();
        var row = await conn.QuerySingleOrDefaultAsync<PurchaseRow>(
            """
            SELECT id, customer_id AS CustomerId, store_id AS StoreId, amount, category, percentage, cashback,
                   created_at AS CreatedAt
            FROM purchases WHERE id = @id
            """, new { id });
        return row?.ToEntity();
    }

    private record PurchaseRow(Guid Id, string CustomerId, string StoreId, decimal Amount, string Category,
        decimal Percentage, decimal Cashback, DateTime CreatedAt)
    {
        public Purchase ToEntity() => new(Id, CustomerId, StoreId, Amount, Category, Percentage, Cashback,
            new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)));
    }

    public async Task<IList<PurchaseSummary>> ListByCustomer(string customerId)
    {
        await using var conn = db.Open();
        var rows = await conn.QueryAsync<PurchaseSummary>(
            """
            SELECT p.id, s.name AS StoreName, p.amount, p.percentage, p.cashback, p.created_at AS CreatedAt
            FROM purchases p JOIN stores s ON s.id = p.store_id
            WHERE p.customer_id = @customerId ORDER BY p.created_at DESC LIMIT 10
            """, new { customerId });
        return rows.ToList();
    }
}

public class RedemptionRepository(PostgresConnection db) : IRedemptionRepository
{
    public async Task Add(Redemption redemption)
    {
        await using var conn = db.Open();
        await conn.ExecuteAsync(
            "INSERT INTO redemptions (id, customer_id, amount, pix_key, created_at) VALUES (@Id, @CustomerId, @Amount, @PixKey, @CreatedAt)",
            redemption);
    }
}
