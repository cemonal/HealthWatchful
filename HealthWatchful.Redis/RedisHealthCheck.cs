using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Redis
{
    /// <summary>
    /// Provides a health check for Redis services, ensuring connectivity and verifying the cluster state if applicable.
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private static readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        private readonly string _redisConnectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class with the specified Redis connection string.
        /// </summary>
        /// <param name="redisConnectionString">The Redis connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="redisConnectionString"/> is null, empty or whitespace.</exception>
        public RedisHealthCheck(string redisConnectionString)
        {
            if (string.IsNullOrWhiteSpace(redisConnectionString))
                throw new ArgumentNullException(nameof(redisConnectionString), "Redis connection string cannot be null or whitespace!");

            _redisConnectionString = redisConnectionString;
        }

        /// <summary>
        /// Performs a health check by connecting to the Redis server and checking the state of the cluster.
        /// </summary>
        /// <param name="context">A context object containing information about the current health check execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with a <see cref="HealthCheckResult"/> containing the health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connections.TryGetValue(_redisConnectionString, out var connection))
                {
                    try
                    {
                        var connectionMultiplexerTask = ConnectionMultiplexer.ConnectAsync(_redisConnectionString);
                        connection = await TimeoutAsync(connectionMultiplexerTask, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return new HealthCheckResult(context.Registration.FailureStatus, description: "Healthcheck timed out");
                    }

                    if (!_connections.TryAdd(_redisConnectionString, connection))
                    {
                        // Dispose new connection which we just created, because we don't need it.
                        connection.Dispose();
                        connection = _connections[_redisConnectionString];
                    }
                }

                foreach (var endPoint in connection.GetEndPoints(configuredOnly: true))
                {
                    var server = connection.GetServer(endPoint);

                    if (server.ServerType != ServerType.Cluster)
                    {
                        await connection.GetDatabase().PingAsync().ConfigureAwait(false);
                        await server.PingAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        var clusterInfo = await server.ExecuteAsync("CLUSTER", "INFO").ConfigureAwait(false);

                        if (clusterInfo is object && !clusterInfo.IsNull)
                        {
                            if (!clusterInfo.ToString().Contains("cluster_state:ok"))
                            {
                                //cluster info is not ok!
                                return new HealthCheckResult(context.Registration.FailureStatus, description: $"INFO CLUSTER is not on OK state for endpoint {endPoint}");
                            }
                        }
                        else
                        {
                            //cluster info cannot be read for this cluster node
                            return new HealthCheckResult(context.Registration.FailureStatus, description: $"INFO CLUSTER is null or can't be read for endpoint {endPoint}");
                        }
                    }
                }

                return HealthCheckResult.Healthy("OK");
            }
            catch (Exception ex)
            {
                _connections.TryRemove(_redisConnectionString, out var connection);
                connection?.Dispose();
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

        // Remove when https://github.com/StackExchange/StackExchange.Redis/issues/1039 is done
        private static async Task<ConnectionMultiplexer> TimeoutAsync(Task<ConnectionMultiplexer> task, CancellationToken cancellationToken)
        {
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);

                if (completedTask == task)
                {
                    timeoutCts.Cancel();
                    return await task.ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException();
            }
        }
    }
}