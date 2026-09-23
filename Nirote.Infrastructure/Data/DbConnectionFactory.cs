using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Nirote.Infrastructure.Data;

public interface IDbConnectionFactory
{
    SqlConnection Create();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("DefaultConnection is not configured.");
        _connectionString = cs;
    }

    public SqlConnection Create() => new(_connectionString);
}
