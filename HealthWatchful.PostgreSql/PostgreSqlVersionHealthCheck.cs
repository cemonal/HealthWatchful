using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using System.Threading;
using System;
using Npgsql;

namespace HealthWatchful.PostgreSql
{
    /// <summary>
    /// A health check for verifying the version of a PostgreSQL database.
    /// </summary>
    public class PostgreSqlVersionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly Version _minimumVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlVersionHealthCheck"/> class
        /// with the specified connection string and minimum required PostgreSQL Server version.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the health check.</param>
        /// <param name="minimumVersion">The minimum required PostgreSQL Server version for the health check.</param>
        public PostgreSqlVersionHealthCheck(string connectionString, string minimumVersion)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _minimumVersion = new Version(minimumVersion);
        }

        /// <summary>
        /// Checks the PostgreSQL Server version by opening a connection to the server and
        /// retrieving the server version number. If the server version is equal to or
        /// greater than the minimum required version, the health check passes.
        /// </summary>
        /// <param name="context">The context of the health check.</param>
        /// <returns>A task representing the asynchronous health check operation.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    var serverVersion = new Version(connection.ServerVersion);

                    if (serverVersion >= _minimumVersion)
                        return HealthCheckResult.Healthy($"Connected to PostgreSQL Server version {serverVersion}.");

                    return HealthCheckResult.Unhealthy($"The minimum required PostgreSQL Server version is {_minimumVersion}. The connected server version is {serverVersion}.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Could not connect to PostgreSQL Server.", ex);
                }
            }
        }
    }
}
