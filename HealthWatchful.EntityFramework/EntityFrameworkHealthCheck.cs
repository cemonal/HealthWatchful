using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.EntityFramework
{
    /// <summary>
    /// Provides a health check for Entity Framework <see cref="DbContext"/> based services.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to be used.</typeparam>
    public class EntityFrameworkHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly string _connectionString;
        private readonly string _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkHealthCheck{TContext}"/> class with the specified <paramref name="connectionString"/> and a default query.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the database.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> is null, empty or whitespace.</exception>
        public EntityFrameworkHealthCheck(string connectionString) : this(connectionString, "SELECT 1;") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkHealthCheck{TContext}"/> class with the specified <paramref name="connectionString"/> and <paramref name="query"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to be used when connecting to the database.</param>
        /// <param name="query">The query to be executed during the health check.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString"/> or <paramref name="query"/> is null, empty or whitespace.</exception>
        public EntityFrameworkHealthCheck(string connectionString, string query)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "ConnectionString cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query), "Query cannot be null or whitespace!");

            _connectionString = connectionString;
            _query = query;
        }

        /// <summary>
        /// Checks the health of the Entity Framework <see cref="DbContext"/> based service by executing the specified query.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create a new instance of the DbContext
                using (var dbContext = (TContext)Activator.CreateInstance(typeof(TContext), _connectionString))
                {
                    // Set the connection string
                    dbContext.Database.Connection.ConnectionString = _connectionString;

                    // Execute the query
                    using (var command = dbContext.Database.Connection.CreateCommand())
                    {
                        command.CommandText = _query;

                        // Open the connection and execute the query
                        await dbContext.Database.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        dbContext.Database.Connection.Close();
                    }

                    // If the query executes successfully, return a healthy HealthCheckResult
                    return HealthCheckResult.Healthy("OK");
                }
            }
            catch (Exception ex)
            {
                // If an exception is thrown, return an unhealthy HealthCheckResult with the exception details
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }
    }
}
