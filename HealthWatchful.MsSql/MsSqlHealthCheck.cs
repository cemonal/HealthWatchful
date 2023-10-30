using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MsSql
{
    /// <summary>
    /// Provides a health check for Microsoft SQL Server services, ensuring connectivity and verifying the database server responsiveness by executing a custom query.
    /// </summary>
    public class MsSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlHealthCheck"/> class with the specified connection string and a default query.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the SQL Server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> is null, empty or whitespace.</exception>
        public MsSqlHealthCheck(string connectionString) : this(connectionString, "SELECT 1;") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlHealthCheck"/> class with the specified connection string and query.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the SQL Server.</param>
        /// <param name="query">The query to be executed for health check purposes.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> or <paramref name="query"/> is null, empty or whitespace.</exception>
        public MsSqlHealthCheck(string connectionString, string query)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "ConnectionString cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query), "Query cannot be null or whitespace!");

            _connectionString = connectionString;
            _query = query;
        }

        /// <summary>
        /// Checks the health of the SQL Server service by attempting to open a connection and executing the specified query.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _query;
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