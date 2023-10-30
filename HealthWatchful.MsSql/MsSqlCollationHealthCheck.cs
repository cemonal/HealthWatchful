using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MsSql
{
    /// <summary>
    /// Provides a health check for Microsoft SQL Server services, ensuring the server's collation matches the expected collation.
    /// </summary>
    public class MsSqlCollationHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _expectedCollation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlCollationHealthCheck"/> class with the specified connection string and expected collation.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the SQL Server.</param>
        /// <param name="expectedCollation ">The expected collation for the SQL Server.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> or <paramref name="expectedCollation"/> is null or whitespace.</exception>
        public MsSqlCollationHealthCheck(string connectionString, string expectedCollation)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "The MSSQL connection string cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(expectedCollation))
                throw new ArgumentNullException(nameof(expectedCollation), "The expected collation value cannot be null or whitespace!");

            _connectionString = connectionString;
            _expectedCollation = expectedCollation;
        }

        /// <summary>
        /// Checks the health of the SQL Server service by verifying the server's collation matches the expected collation.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result = HealthCheckResult.Healthy("OK");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT SERVERPROPERTY('collation');";

                        var actualCollation = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;

                        if (!string.Equals(actualCollation, _expectedCollation, StringComparison.InvariantCultureIgnoreCase))
                            result = new HealthCheckResult(context.Registration.FailureStatus, $"MSSQL collation is incorrect. Actual collation: '{actualCollation}', expected collation: '{_expectedCollation}'.");
                    }
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
