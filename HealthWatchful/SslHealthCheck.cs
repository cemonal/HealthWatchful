using HealthWatchful.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that monitors SSL certificate validity and expiration.
    /// </summary>
    public class SslHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _checkLeftDays;
        private readonly AddressFamily _addressFamily;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslHealthCheck"/> class with default settings.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SslHealthCheck(string host) : this(host, 443, 60, AddressFamily.InterNetwork) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslHealthCheck"/> class with the specified host and port.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SslHealthCheck(string host, int port) : this(host, port, 60, AddressFamily.InterNetwork) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslHealthCheck"/> class with the specified host, port, and address family.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SslHealthCheck(string host, int port, AddressFamily addressFamily) : this(host, port, 60, addressFamily) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslHealthCheck"/> class with the specified host, port, check left days, and address family.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SslHealthCheck(string host, int port, int checkLeftDays, AddressFamily addressFamily)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            _host = host;
            _port = port;
            _checkLeftDays = checkLeftDays;
            _addressFamily = addressFamily;
        }

        /// <summary>
        /// Checks the health of the SSL certificate by monitoring its validity and expiration.
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
                { "Port", _port },
                { "AddressFamily", _addressFamily.ToString() }
            };

            try
            {
                using (var tcpClient = new TcpClient(_addressFamily))
                {
#if NET5_0_OR_GREATER
                    await tcpClient.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
#else
                    await tcpClient.ConnectAsync(_host, _port).WithCancellationTokenAsync(cancellationToken).ConfigureAwait(false);
#endif
                    if (!tcpClient.Connected)
                        result = new HealthCheckResult(context.Registration.FailureStatus, description: $"Connection to host {_host}:{_port} failed");

                    var certificate = await GetSslCertificateAsync(tcpClient).ConfigureAwait(false);

                    if (certificate is null || !certificate.Verify())
                        result = new HealthCheckResult(context.Registration.FailureStatus, description: $"Ssl certificate not present or not valid for {_host}:{_port}");
                    else if (certificate.NotAfter.Subtract(DateTime.Now).TotalDays > _checkLeftDays && certificate.NotAfter.Subtract(DateTime.Now).TotalDays < _checkLeftDays * 1.5)
                        result = new HealthCheckResult(HealthStatus.Degraded, description: $"Ssl certificate for {_host}:{_port} is about to expire in {_checkLeftDays} days");
                    else if (certificate.NotAfter.Subtract(DateTime.Now).TotalDays <= _checkLeftDays)
                        result = new HealthCheckResult(context.Registration.FailureStatus, description: $"Ssl certificate for {_host}:{_port} is about to expire in {_checkLeftDays} days");
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

        private async Task<X509Certificate2> GetSslCertificateAsync(TcpClient client)
        {
            var ssl = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback((sender, cert, ca, sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None), null);

            try
            {
                await ssl.AuthenticateAsClientAsync(_host).ConfigureAwait(false);
                var cert = ssl.RemoteCertificate;
                return cert == null ? null : new X509Certificate2(cert);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                ssl.Close();
            }
        }
    }
}