using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// A health check implementation that verifies the ability to connect to an LDAP server.
    /// </summary>
    public class LdapHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapHealthCheck"/> class with the specified host.
        /// </summary>
        /// <param name="host">The LDAP server host.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public LdapHealthCheck(string host) : this(host, 389, 30) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapHealthCheck"/> class with the specified host and port.
        /// </summary>
        /// <param name="host">The LDAP server host.</param>
        /// <param name="port">The LDAP server port.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public LdapHealthCheck(string host, int port) : this(host, port, 30) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapHealthCheck"/> class with the specified host, port, and timeout.
        /// </summary>
        /// <param name="host">The LDAP server host.</param>
        /// <param name="port">The LDAP server port.</param>
        /// <param name="timeout">The timeout for the LDAP connection in seconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public LdapHealthCheck(string host, int port, int timeout)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            var hostArray = host.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            _host = hostArray[0];
            _port = hostArray.Count() > 1 ? (int.TryParse(hostArray[1], out int p) ? p : port) : port;
            _timeout = timeout;
        }

        /// <summary>
        /// Verifies the ability to connect to an LDAP server by binding to the server with anonymous authentication.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result = HealthCheckResult.Healthy("OK");

            var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(_host + ":" + _port));
            var ts = new TimeSpan(0, 0, 0, _timeout);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ldapConnection.AuthType = AuthType.Anonymous;
                ldapConnection.AutoBind = false;
                ldapConnection.Timeout = ts;
                ldapConnection.Bind();

                //More than 2/3 of the timeout?
                if (stopwatch.Elapsed.TotalSeconds > (_timeout * 2 / 3))
                    result = HealthCheckResult.Degraded();
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }
            finally
            {
                ldapConnection.Dispose();
            }

            return Task.FromResult(result);
        }
    }
}
