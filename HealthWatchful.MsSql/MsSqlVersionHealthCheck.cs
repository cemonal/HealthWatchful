using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MsSql
{
    /// <summary>
    /// A health check for verifying the version of a SQL Server database.
    /// </summary>
    public class MsSqlVersionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly Version _minimumVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlVersionHealthCheck"/> class
        /// with the specified connection string and minimum required SQL Server version.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the health check.</param>
        /// <param name="minimumVersion">The minimum required SQL Server version for the health check.</param>
        public MsSqlVersionHealthCheck(string connectionString, string minimumVersion)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _minimumVersion = new Version(minimumVersion);
        }

        /// <summary>
        /// Checks the SQL Server version by opening a connection to the server and
        /// retrieving the server version number. If the server version is equal to or
        /// greater than the minimum required version, the health check passes.
        /// </summary>
        /// <param name="context">The context of the health check.</param>
        /// <returns>A task representing the asynchronous health check operation.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    var serverVersion = new Version(connection.ServerVersion);

                    if (serverVersion >= _minimumVersion)
                        return HealthCheckResult.Healthy($"Connected to SQL Server version {serverVersion}.");

                    return HealthCheckResult.Unhealthy($"The minimum required SQL Server version is {_minimumVersion}. The connected server version is {serverVersion}.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Could not connect to SQL Server.", ex);
                }
            }
        }
    }
}