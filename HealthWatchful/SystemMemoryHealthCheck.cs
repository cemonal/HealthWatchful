using HealthWatchful.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that monitors system memory usage.
    /// </summary>
    public class SystemMemoryHealthCheck : IHealthCheck
    {
        private readonly MemoryMetricsService _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMemoryHealthCheck"/> class with the specified shell execution mode.
        /// </summary>
        /// <param name="useShellExecute">Indicates whether to use shell execution to retrieve memory metrics.</param>
        public SystemMemoryHealthCheck(bool useShellExecute)
        {
            _client = new MemoryMetricsService(useShellExecute);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMemoryHealthCheck"/> class with default settings.
        /// </summary>
        public SystemMemoryHealthCheck()
        {
            _client = new MemoryMetricsService();
        }

        /// <summary>
        /// Checks the health of the system by monitoring memory usage.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {
                var metrics = await _client.GetMetricsAsync(cancellationToken).ConfigureAwait(false);
                var percentUsed = 100 * metrics.Used / metrics.Total;

                var status = HealthStatus.Healthy;

                if (percentUsed > 80)
                    status = HealthStatus.Degraded;

                if (percentUsed > 90)
                    status = HealthStatus.Unhealthy;

                var data = new Dictionary<string, object>
                {
                    { "Total", metrics.Total },
                    { "Used", metrics.Used },
                    { "Free", metrics.Free }
                };

                result = new HealthCheckResult(status, "Memory usage: " + string.Format("{0:0.0}", percentUsed) + "%", null, data);
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }

            return result;
        }
    }
}