using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MySql
{
    /// <summary>
    /// A health check for verifying the version of a MySQL Server database.
    /// </summary>
    public class MySqlVersionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly Version _minimumVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlVersionHealthCheck"/> class
        /// with the specified connection string and minimum required MySQL Server version.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the health check.</param>
        /// <param name="minimumVersion">The minimum required MySQL Server version for the health check.</param>
        public MySqlVersionHealthCheck(string connectionString, string minimumVersion)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _minimumVersion = new Version(minimumVersion);
        }

        /// <summary>
        /// Checks the MySQL Server version by opening a connection to the server and
        /// retrieving the server version number. If the server version is equal to or
        /// greater than the minimum required version, the health check passes.
        /// </summary>
        /// <param name="context">The context of the health check.</param>
        /// <returns>A task representing the asynchronous health check operation.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    var serverVersion = new Version(connection.ServerVersion);

                    if (serverVersion >= _minimumVersion)
                        return HealthCheckResult.Healthy($"Connected to MySQL Server version {serverVersion}.");

                    return HealthCheckResult.Unhealthy($"The minimum required MySQL Server version is {_minimumVersion}. The connected server version is {serverVersion}.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Could not connect to MySQL Server.", ex);
                }
            }
        }
    }
}
