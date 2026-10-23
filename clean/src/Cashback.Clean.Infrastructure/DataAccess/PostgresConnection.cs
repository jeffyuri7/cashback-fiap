using Npgsql;

namespace Cashback.Clean.Infrastructure.DataAccess;

/// <summary>Abre conexões com o PostgreSQL. Só a infraestrutura sabe que existe um banco.</summary>
public class PostgresConnection(string connectionString)
{
    public NpgsqlConnection Open() => new(connectionString);
}
