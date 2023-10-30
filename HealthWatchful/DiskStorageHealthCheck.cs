using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that monitors the available free disk space.
    /// </summary>
    public class DiskStorageHealthCheck : IHealthCheck
    {
        private readonly long _limit;

        // <summary>
        /// Initializes a new instance of the <see cref="DiskStorageHealthCheck"/> class.
        /// </summary>
        /// <param name="limit">The minimum amount of available free space (in MB) before reporting unhealthy or degraded status.</param>
        public DiskStorageHealthCheck(long limit) => _limit = limit;

        /// <summary>
        /// Checks the health of the system's available free disk space.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {
                var driverName = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);

                DriveInfo driver = DriveInfo.GetDrives().First(x => string.Equals(x.Name, driverName, StringComparison.InvariantCultureIgnoreCase));

                if (!driver.IsReady)
                    throw new Exception("Driver was not ready!");

                var data = new Dictionary<string, object>
                {
                    { "Name", driver.Name },
                    { "AvailableFreeSpace", driver.AvailableFreeSpace },
                    { "TotalSize", driver.TotalSize }
                };

                string description = $"Available free space: {ConvertBytesToMegabytes(driver.AvailableFreeSpace):0.00} MB";
                long unhealthyLimit = ConvertMegabytesToBytes(_limit);

                if (driver.AvailableFreeSpace <= unhealthyLimit)
                    result = new HealthCheckResult(context.Registration.FailureStatus, description: description, data: data);
                else if (driver.AvailableFreeSpace <= (unhealthyLimit / 2 * 3))
                    result = new HealthCheckResult(HealthStatus.Degraded, description: description, data: data);
                else
                    result = new HealthCheckResult(HealthStatus.Healthy, description, data: data);
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }

            return Task.FromResult(result);
        }

        private static long ConvertBytesToMegabytes(long bytes) => bytes / 1024L / 1024L;

        private static long ConvertMegabytesToBytes(long megabytes) => megabytes * 1024L * 1024L;
    }
}
