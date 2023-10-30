using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.EntityFrameworkCore
{
    /// <summary>
    /// Provides a health check for Entity Framework Core <see cref="DbContext"/> based services.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    public class EntityFrameworkCoreHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly DbContextOptionsBuilder _optionsBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkCoreHealthCheck{TContext}"/> class.
        /// </summary>
        /// <param name="optionsBuilder">The DbContext options builder.</param>
        /// <exception cref="ArgumentNullException">Thrown when optionsBuilder is null.</exception>
        public EntityFrameworkCoreHealthCheck(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder), "DbContextOptionsBuilder cannot be null!");

            _optionsBuilder = optionsBuilder;
        }

        /// <summary>
        /// Checks the health of the Entity Framework Core database connectivity.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous health check operation.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var dbContext = CreateDbContext())
                {
                    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

                    if (!canConnect)
                        return HealthCheckResult.Unhealthy($"{typeof(TContext).Name} connection to the database failed.");

                    return HealthCheckResult.Healthy("OK");
                }
            }
            catch (Exception ex)
            {
                // If an exception is thrown, return an unhealthy HealthCheckResult with the exception details
                return new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
        }

        private TContext CreateDbContext()
        {
            return (TContext)Activator.CreateInstance(typeof(TContext), _optionsBuilder.Options);
        }
    }
}