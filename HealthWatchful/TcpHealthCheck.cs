using HealthWatchful.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// TcpHealthCheck is a class that implements a TCP health check.
    /// </summary>
    public class TcpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly AddressFamily _addressFamily;

        /// <summary>
        /// Main constructor for the <see cref="TcpHealthCheck"/> class.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <param name="addressFamily">The address family to use for the connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public TcpHealthCheck(string host, int port, AddressFamily addressFamily)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            _host = host;
            _port = port;
            _addressFamily = addressFamily;
        }

        /// <summary>
        /// Overloaded constructor for the <see cref="TcpHealthCheck"/> class, using AddressFamily.InterNetwork as the default address family.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public TcpHealthCheck(string host, int port) : this(host, port, AddressFamily.InterNetwork) { }

        /// <summary>
        /// Checks the health of the TCP connection.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            var data = new Dictionary<string, object>
            {
                { "Host", _host },
                { "Port", _port }
            };

            try
            {
                using (TcpClient client = new TcpClient(_addressFamily))
                {
#if NET5_0_OR_GREATER
                    await client.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
#else
                    await client.ConnectAsync(_host, _port).WithCancellationTokenAsync(cancellationToken).ConfigureAwait(false);
#endif

                    if (!client.Connected)
                        result = new HealthCheckResult(HealthStatus.Unhealthy, "Not OK", null, data);
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
