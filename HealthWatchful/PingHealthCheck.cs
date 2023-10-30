using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that performs a ping to a specified host.
    /// </summary>
    public class PingHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="PingHealthCheck"/> class.
        /// </summary>
        /// <param name="host">The host to ping.</param>
        /// <param name="timeout">The timeout value for the ping operation in milliseconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public PingHealthCheck(string host, int timeout)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            _host = host;
            _timeout = timeout;
        }

        /// <summary>
        /// Checks the health of the system by pinging the specified host.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            var data = new Dictionary<string, object>
            {
                { "Host", _host },
                { "Timeout", _timeout }
            };

            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(_host, _timeout).ConfigureAwait(false);

                    if (reply.Status != IPStatus.Success)
                        result = new HealthCheckResult(HealthStatus.Unhealthy, reply.Status.ToString(), null, data);
                    else if (reply.RoundtripTime >= _timeout)
                        result = new HealthCheckResult(HealthStatus.Degraded, "Timeout", null, data);
                    else
                        result = new HealthCheckResult(HealthStatus.Healthy, "OK", null, data);
                }
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex, data: data);
            }

            return result;
        }
    }
}
