using System.Data;
using Npgsql;
using Polly;
using Polly.Retry;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Services
{
    /// <summary>
    /// A robust, production-ready PostgreSQL database helper using Npgsql and Polly for resilience.
    /// Supports async operations, retry logic, logging, connection pooling, and strong typing.
    /// Configured exclusively via environment variables.
    /// </summary>
    public sealed class PostgresDb : IPostgresDbData, IDisposable, IAsyncDisposable
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<PostgresDb> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private bool _disposed = false;

        public PostgresDb(ILogger<PostgresDb> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Build connection string from environment variables
            string connectionString = BuildConnectionStringFromEnvironment();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseLoggerFactory(LoggerFactory.Create(b => b.AddConsole()));
            _dataSource = dataSourceBuilder.Build();

            // Exponential backoff + jitter retry policy for transient failures
            _retryPolicy = Policy
                .Handle<NpgsqlException>(ex => ex.IsTransient)
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)) +
                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: (exception, timeSpan, attempt, _) =>
                        _logger.LogWarning(
                            exception,
                            "Attempt {Attempt} failed executing PostgreSQL command. Retrying in {Delay}ms.",
                            attempt, timeSpan.TotalMilliseconds));
        }

        // Build connection string from environment variables
        private static string BuildConnectionStringFromEnvironment()
        {
            var host     = GetRequired("DB_PTGR_HOST");
            var port     = GetRequired("DB_PTGR_PORT");
            var database = "dbunipos";
            var username = GetRequired("DB_PTGR_USER");
            var password = GetRequired("DB_PTGR_PASS");

            var csb = new NpgsqlConnectionStringBuilder
            {
                Host                   = host,
                Port                   = int.Parse(port),
                Database               = database,
                Username               = username,
                Password               = password,

                Pooling                = true,
                MinPoolSize            = 5,
                MaxPoolSize            = 100,
                Timeout                = 15,
                CommandTimeout         = 30,

                // Security defaults
                SslMode                = SslMode.Prefer
            };

            return csb.ConnectionString;
        }

        private static string GetRequired(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Environment variable '{name}' is required but missing.");
            return value;
        }

        public NpgsqlConnection CreateConnection()
        {
            var conn = _dataSource.OpenConnection();
            return conn;
        }

        // Execute Scalar (COUNT, SUM, etc...)
        public async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken, params NpgsqlParameter[] parameters)
        {
            var result = await ExecuteAsync(
                cmd => cmd.ExecuteScalarAsync(cancellationToken),
                sql,
                parameters ?? Array.Empty<NpgsqlParameter>());
            return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await ExecuteScalarAsync<int>("SELECT 1", cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Core execution with retry + logging
        private async Task<T> ExecuteAsync<T>(
            Func<NpgsqlCommand, Task<T>> executeFunc,
            string sql,
            NpgsqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await using var cmd = _dataSource.CreateCommand(sql);
                if (parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                cmd.CommandTimeout = 30;

                _logger.LogDebug("Executing SQL: {Sql} | Parameters: {@Parameters}",
                    sql,
                    parameters.Select(p => new { p.ParameterName, p.Value }));

                try
                {
                    return await executeFunc(cmd);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error executing SQL command: {Sql}", sql);
                    throw;
                }
            });
        }

        // Disposal
        public void Dispose()
        {
            if (!_disposed)
            {
                _dataSource?.Dispose();
                _disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_dataSource is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else
                    _dataSource?.Dispose();

                _disposed = true;
            }

            // Suppress finalization
            GC.SuppressFinalize(this);
        }
    }
}