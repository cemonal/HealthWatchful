using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MongoDb
{
    /// <summary>
    /// Represents a health check that verifies read and write operations to a MongoDB collection.
    /// </summary>
    public class MongoReadWriteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly string _collectionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoReadWriteHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string for the MongoDB database.</param>
        /// <param name="databaseName">The name of the database to check.</param>
        /// <param name="collectionName">The name of the collection to read and write from.</param>
        /// <param name="timeout">The timeout for the health check.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the constructor parameters are null, empty or whitespace.</exception>
        public MongoReadWriteHealthCheck(string connectionString, string databaseName, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException(nameof(databaseName), "Database name cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentNullException(nameof(collectionName), "Collection name cannot be null or whitespace!");

            _connectionString = connectionString;
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        /// <summary>
        /// Checks the health of the MongoDB server by performing a read and write operation.
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
                var collection = database.GetCollection<BsonDocument>(_collectionName);
                var document = new BsonDocument("name", "test");
                await collection.InsertOneAsync(document, null, cancellationToken).ConfigureAwait(false);
                var result = await collection.Find(document).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (result == null)
                    return HealthCheckResult.Unhealthy("MongoDB read/write operation failed.");

                await collection.DeleteOneAsync(document, cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy("OK");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}
