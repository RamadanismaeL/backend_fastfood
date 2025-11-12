using System.Data;
using Npgsql;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IPostgresDbData
    {
        NpgsqlConnection CreateConnection();
        Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken, params NpgsqlParameter[] parameters);
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }
}