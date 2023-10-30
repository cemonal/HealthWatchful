
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that monitors CPU usage on a Windows system.
    /// </summary>
    public class CpuUsageHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CpuUsageHealthCheck"/> class.
        /// </summary>
        public CpuUsageHealthCheck()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException("CPU health check is only available on Windows OS!");
        }

        /// <summary>
        /// Checks the health of the system's CPU usage.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var cpuUsage = cpuCounter.NextValue();

                var status = HealthStatus.Healthy;

                if (cpuUsage > 80)
                    status = HealthStatus.Degraded;

                if (cpuUsage > 90)
                    status = HealthStatus.Unhealthy;

                var data = new Dictionary<string, object>
                {
                    { "Used", cpuUsage }
                };

                result = new HealthCheckResult(status, "CPU usage: " + string.Format("{0:0.0}", cpuUsage) + "%", null, data);
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }

            return Task.FromResult(result);
        }
    }
}
