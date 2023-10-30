using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Threading;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check for a Windows service.
    /// </summary>
    public class WindowsServiceHealthCheck : IHealthCheck
    {
        private readonly string _serviceName;
        private readonly Func<ServiceController, bool> _predicate;
        private readonly string _machineName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsServiceHealthCheck"/> class with the specified service name, predicate, and machine name.
        /// </summary>
        /// <param name="serviceName">The name of the Windows service to check.</param>
        /// <param name="predicate">A function that evaluates the health of the service based on its ServiceController.</param>
        /// <param name="machineName">The name of the machine where the service is running. If not provided, defaults to the local machine.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceName"/> or <paramref name="predicate"/> is null, empty or whitespace.</exception>
        public WindowsServiceHealthCheck(string serviceName, Func<ServiceController, bool> predicate, string machineName = default)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName), "Service name cannot be null or whitespace!");

            _serviceName = serviceName;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null!");
            _machineName = machineName;
        }

        /// <summary>
        /// Checks the health of the Windows service and returns a <see cref="HealthCheckResult"/>.
        /// </summary>
        /// <param name="context">A context object containing information about the health check, such as the registered failure status.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var sc = GetServiceController())
                {
                    if (_predicate(sc))
                        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "OK"));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, exception: ex));
            }

            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus));
        }

        /// <summary>
        /// Gets a ServiceController for the specified service.
        /// </summary>
        /// <returns>A ServiceController object for the specified service.</returns>
        private ServiceController GetServiceController() =>
            !string.IsNullOrWhiteSpace(_machineName)
                ? new ServiceController(_serviceName, _machineName)
                : new ServiceController(_serviceName);
    }
}