using System.Data;
using Npgsql;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Persistence;

public sealed class SqlConnectionFactory : ISqlConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private IDbConnection? _connection;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        if (_connection != null && _connection.State == ConnectionState.Open) return _connection;
        if (_connection != null && _connection.State != ConnectionState.Open) _connection.Dispose();

        _connection = new NpgsqlConnection(_connectionString);
        _connection.Open();
        return _connection;
    }

    public void Dispose()
    {
        if (_connection != null && _connection.State == ConnectionState.Open) _connection.Dispose();
    }
}
