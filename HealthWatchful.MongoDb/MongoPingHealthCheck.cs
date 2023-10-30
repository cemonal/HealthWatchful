using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MongoDb
{
    /// <summary>
    /// A health check implementation that pings a MongoDB database to check its health.
    /// </summary>
    public class MongoPingHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoPingHealthCheck"/> class with the specified connection string and database name.
        /// </summary>
        /// <param name="connectionString">The connection string for the MongoDB database.</param>
        /// <param name="databaseName">The name of the database to check. If null, the database name is extracted from the connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> is null, empty or whitespace.</exception>
        public MongoPingHealthCheck(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or whitespace!");

            _connectionString = connectionString;

            if (string.IsNullOrEmpty(databaseName))
                _databaseName = MongoUrl.Create(connectionString)?.DatabaseName;
            else
                _databaseName = databaseName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoPingHealthCheck"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string for the MongoDB database.</param>
        public MongoPingHealthCheck(string connectionString) : this(connectionString, null) { }

        /// <summary>
        /// Checks the health of the MongoDB database by sending a ping command to the database.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new MongoClient(_connectionString);
                var database = client.GetDatabase(_databaseName);

                var command = new BsonDocument("ping", 1);
                var result = await database.RunCommandAsync<BsonDocument>(command, null, cancellationToken).ConfigureAwait(false);

                if (result.Contains("ok") && result["ok"] == 1.0)
                    return HealthCheckResult.Healthy("OK");
                else
                    return HealthCheckResult.Unhealthy("Not OK");
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }
    }
}