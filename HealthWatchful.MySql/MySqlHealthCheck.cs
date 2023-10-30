using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MySql
{
    /// <summary>
    /// Provides a health check for MySQL services, ensuring connectivity and verifying the database server responsiveness.
    /// </summary>
    public class MySqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlHealthCheck"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the MySQL server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> is null, empty or whitespace.</exception>
        public MySqlHealthCheck(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// Checks the health of the MySQL service by attempting to open a connection and executing a PING command.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    result = await connection.PingAsync(cancellationToken).ConfigureAwait(false)
                        ? HealthCheckResult.Healthy("OK")
                        : new HealthCheckResult(context.Registration.FailureStatus, description: $"The {nameof(MySqlHealthCheck)} check fail.");
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