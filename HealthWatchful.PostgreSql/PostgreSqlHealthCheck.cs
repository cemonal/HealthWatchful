using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.PostgreSql
{
    public class PostgreSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;

        public PostgreSqlHealthCheck(string connectionString) : this(connectionString, "SELECT 1;") { }

        public PostgreSqlHealthCheck(string connectionString, string query)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "ConnectionString cannot be null or empty!");

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query), "Query cannot be null or empty!");

            _connectionString = connectionString;
            _query = query;
        }

        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (var command = new NpgsqlCommand(_query, connection))
                    {
                        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    }

                    return HealthCheckResult.Healthy("OK");
                }
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }
    }
}
