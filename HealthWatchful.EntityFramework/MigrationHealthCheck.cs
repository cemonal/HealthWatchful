using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.EntityFramework
{
    /// <summary>
    /// A health check implementation for Entity Framework migrations.
    /// </summary>
    public class MigrationHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private DbMigrationsConfiguration<TContext> _configuration;
        private bool _isHealthy = false;

        public MigrationHealthCheck(DbMigrationsConfiguration<TContext> configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Checks the health of the Entity Framework migrations in the specified <see cref="DbContext"/>. If any migrations are pending, 
        /// the method returns an unhealthy result. If no migrations are pending and no exceptions occur, the method returns a healthy result.
        /// </summary>
        /// <param name="context">A <see cref="HealthCheckContext"/> object that contains context data for performing the health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, containing a <see cref="HealthCheckResult"/> indicating 
        /// the health status of the Entity Framework migrations in the <see cref="DbContext"/>.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // If the migration check was already successful, return healthy result immediately
                if (_isHealthy)
                    return HealthCheckResult.Healthy("OK");

                // Check if there are any pending migrations
                var pendingMigrationsCount = 0;
                var retryCount = 3;
                var retryDelay = TimeSpan.FromSeconds(10);

                for (var i = 0; i < retryCount; i++)
                {
                    try
                    {
                        // Create a new DbMigrator object to check for pending migrations
                        var migrator = new DbMigrator(_configuration);

                        // Get the list of pending migrations asynchronously
                        var pendingMigrations = await Task.Run(() => migrator.GetPendingMigrations(), cancellationToken);

                        // If there are pending migrations, set the cache and return degraded result
                        if (pendingMigrations != null && pendingMigrations.Any())
                        {
                            pendingMigrationsCount = pendingMigrations.Count();
                        }
                        // If there are no pending migrations, set the cache, update the status and return healthy result
                        else
                        {
                            _isHealthy = true;
                            break;
                        }
                    }
                    // If there is an exception during the migration check, return unhealthy result
                    catch (Exception ex)
                    {
                        _isHealthy = false;

                        return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
                    }

                    // Wait for retryDelay before trying again
                    await Task.Delay(retryDelay).ConfigureAwait(false);
                }

                if (!_isHealthy)
                    return HealthCheckResult.Unhealthy($"The database schema is out of date and migration is pending. Pending migrations count: {pendingMigrationsCount}");

                return HealthCheckResult.Healthy("OK");
            }
            catch (Exception ex)
            {
                // If there is an exception during the migration check, return unhealthy result
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }
    }
}