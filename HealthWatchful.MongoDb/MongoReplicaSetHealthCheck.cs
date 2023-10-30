using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.MongoDb
{
    /// <summary>
    /// Represents a health check that verifies the health of a MongoDB replica set by verifying that all members are reachable and synchronized.
    /// </summary>
    public class MongoReplicaSetHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoReplicaSetHealthCheck"/> class with the specified MongoDB connection string.
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string to use for the health check.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> is null, empty or whitespace.</exception>
        public MongoReplicaSetHealthCheck(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or whitespace!");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Checks the health of a MongoDB replica set by verifying that all the members are reachable and synchronized.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new MongoClient(_connectionString);
                var database = client.GetDatabase("admin");
                var command = new BsonDocument("replSetGetStatus", 1);
                var result = await database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
                var members = result["members"].AsBsonArray.Select(x => new
                {
                    State = x["stateStr"].AsString,
                    IsMaster = x["ismaster"].AsBoolean,
                    IsSecondary = x["secondary"].AsBoolean,
                    IsHidden = x["hidden"].AsBoolean,
                    IsArbiter = x["arbiterOnly"].AsBoolean,
                    LastHeartbeat = x["lastHeartbeat"].ToUniversalTime()
                });

                var synced = members.All(x => x.State == "PRIMARY" || x.State == "SECONDARY");
                var reachable = members.All(x => x.State != "UNKNOWN");
                var masters = members.Where(x => x.IsMaster);
                var secondaries = members.Where(x => x.IsSecondary);
                var arbiters = members.Where(x => x.IsArbiter);
                var hidden = members.Where(x => x.IsHidden);
                var message = $"Found {masters.Count()} master(s), {secondaries.Count()} secondary(s), {arbiters.Count()} arbiter(s), and {hidden.Count()} hidden member(s).";

                if (!synced)
                    return HealthCheckResult.Unhealthy("MongoDB replica set is not synchronized.");

                if (!reachable)
                    return HealthCheckResult.Unhealthy("MongoDB replica set has unreachable members.");

                return HealthCheckResult.Healthy(message);
            }            
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }
    }
}
