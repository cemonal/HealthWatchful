using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.PostgreSql
{
    public class PostgreSqlCollationHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _expectedCollation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlCollationHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the PostgreSQL database.</param>
        /// <param name="expectedCollation">The collation that is expected to be used by the PostgreSQL database.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> or <paramref name="expectedCollation"/> is null or whitespace.</exception>
        public PostgreSqlCollationHealthCheck(string connectionString, string expectedCollation)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "The PostgreSQL connection string cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(expectedCollation))
                throw new ArgumentNullException(nameof(expectedCollation), "The expected collation value cannot be null or whitespace!");

            _connectionString = connectionString;
            _expectedCollation = expectedCollation;
        }

        /// <summary>
        /// Performs a health check by verifying that the collation used by the PostgreSQL database matches the expected collation.
        /// </summary>
        /// <param name="context">The context for the health check.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the health check.</param>
        /// <returns>A <see cref="HealthCheckResult"/> indicating the status of the health check.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result = HealthCheckResult.Healthy("OK");

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    string query = "SELECT datcollate FROM pg_database WHERE datname = current_database()";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        string actualCollation = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;

                        if (!string.Equals(actualCollation, _expectedCollation, StringComparison.InvariantCultureIgnoreCase))
                            result = new HealthCheckResult(context.Registration.FailureStatus, $"PostgreSQL collation is incorrect. Actual collation: '{actualCollation}', expected collation: '{_expectedCollation}'");
                    }
                }
                catch (Exception ex)
                {
                    result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
                }

                return result;
            }
        }
    }
}
