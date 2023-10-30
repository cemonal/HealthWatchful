using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.EntityFrameworkCore
{
    /// <summary>
    /// Represents a health check for database migration status.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    public class MigrationHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly DbContextOptionsBuilder _optionsBuilder;
        private bool _isHealthy = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationHealthCheck{TContext}"/> class.
        /// </summary>
        /// <param name="optionsBuilder">The DbContext options builder.</param>
        /// <exception cref="ArgumentNullException">Thrown when optionsBuilder is null.</exception>
        public MigrationHealthCheck(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder), "DbContextOptionsBuilder cannot be null!");

            _optionsBuilder = optionsBuilder;
        }

        /// <summary>
        /// Checks the health of the migration by verifying the database schema status.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous health check operation.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_isHealthy)
                    return HealthCheckResult.Healthy("OK");

                var isHealthy = false;
                var pendingMigrationsCount = 0;
                var retryCount = 3;
                var retryDelay = TimeSpan.FromSeconds(5);

                for (var i = 0; i < retryCount; i++)
                {
                    try
                    {
                        using (var dbContext = CreateDbContext())
                        {
                            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);

                            if (pendingMigrations.Any())
                            {
                                isHealthy = false;
                                pendingMigrationsCount = pendingMigrations.Count();
                            }
                            else
                            {
                                isHealthy = true;
                                _isHealthy = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isHealthy = false;
                        _isHealthy = false;
                        return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
                    }

                    await Task.Delay(retryDelay).ConfigureAwait(false);
                }

                if (!isHealthy)
                    return HealthCheckResult.Unhealthy($"The database schema is out of date and migration is pending. Pending migrations count: {pendingMigrationsCount}");

                return HealthCheckResult.Healthy("OK");
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }

        private TContext CreateDbContext()
        {
            return (TContext)Activator.CreateInstance(typeof(TContext), _optionsBuilder.Options);
        }
    }
}
