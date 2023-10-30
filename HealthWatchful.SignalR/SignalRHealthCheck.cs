using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.SignalR
{
    /// <summary>
    /// A health check implementation that verifies the ability to establish a SignalR <see cref="HubConnection"/>.
    /// </summary>
    public class SignalRHealthCheck : IHealthCheck
    {
        private readonly Func<HubConnection> _hubConnectionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRHealthCheck"/> class with the specified hub connection builder.
        /// </summary>
        /// <param name="hubConnectionBuilder">A delegate that creates a new instance of <see cref="HubConnection"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="hubConnectionBuilder"/> is null.</exception>
        public SignalRHealthCheck(Func<HubConnection> hubConnectionBuilder)
        {
            if (hubConnectionBuilder == null)
                throw new ArgumentNullException(nameof(hubConnectionBuilder));

            _hubConnectionBuilder = hubConnectionBuilder;
        }

        /// <summary>
        /// Verifies the ability to establish a SignalR HubConnection by starting and stopping the connection.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HubConnection connection = null;
            HealthCheckResult result;

            try
            {
                connection = _hubConnectionBuilder();

                await connection.StartAsync(cancellationToken).ConfigureAwait(false);

                result = new HealthCheckResult(HealthStatus.Healthy, "OK");
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
            finally
            {
                if (connection != null)
                    await connection.DisposeAsync().ConfigureAwait(false);
            }

            return result;
        }
    }
}
