using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that determines the running status of the application.
    /// </summary>
    public class ApplicationStatusHealthCheck : IHealthCheck, IDisposable
    {
        private readonly IHostApplicationLifetime _lifetime;
        private CancellationTokenRegistration _ctRegistration = default;
        private bool IsApplicationRunning => _ctRegistration != default;

        public ApplicationStatusHealthCheck(IHostApplicationLifetime lifetime)
        {
            if (lifetime == null)
                throw new ArgumentNullException(nameof(lifetime));

            _lifetime = lifetime;
            _ctRegistration = _lifetime.ApplicationStopping.Register(OnStopping);
        }

        /// <summary>
        /// Handler that will be triggered on application stopping event.
        /// </summary>
        private void OnStopping()
        {
            Dispose();
        }

        /// <summary>
        /// Performs the health check by determining if the application is running or stopped.
        /// </summary>
        /// <param name="context">A context object associated with the current health check.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(IsApplicationRunning ? HealthCheckResult.Healthy("OK") : HealthCheckResult.Unhealthy("Stopped"));
        }

        /// <summary>
        /// Disposes the health check, releasing any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _ctRegistration.Dispose();
            _ctRegistration = default;
        }
    }
}
