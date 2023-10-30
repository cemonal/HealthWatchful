using HealthWatchful.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that monitors an FTP server's availability and connectivity.
    /// </summary>
    public class FtpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly bool _createFile;
        private readonly NetworkCredential _credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpHealthCheck"/> class with the specified host.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public FtpHealthCheck(string host) : this(host, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpHealthCheck"/> class with the specified host and createFile flag.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public FtpHealthCheck(string host, bool createFile) : this(host, createFile, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpHealthCheck"/> class with the specified host and credentials.
        /// </summary
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public FtpHealthCheck(string host, NetworkCredential credentials) : this(host, false, credentials) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpHealthCheck"/> class with the specified host, createFile flag, and credentials.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public FtpHealthCheck(string host, bool createFile, NetworkCredential credentials)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            _host = host;
            _credentials = credentials;
            _createFile = createFile;
        }

        /// <summary>
        /// Checks the health of the FTP server by monitoring its availability and connectivity.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            var data = new Dictionary<string, object>
            {
                { "Host", _host }
            };

            try
            {
                var ftpRequest = CreateFtpWebRequest(_host, _createFile, _credentials);

                using (var ftpResponse = (FtpWebResponse)await ftpRequest.GetResponseAsync().WithCancellationTokenAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (ftpResponse.StatusCode != FtpStatusCode.PathnameCreated && ftpResponse.StatusCode != FtpStatusCode.ClosingData)
                        throw new Exception($"Error connecting to ftp host {_host} with exit code {ftpResponse.StatusCode}");
                }

                result = new HealthCheckResult(HealthStatus.Healthy, "OK", null, data);
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex, data: data);
            }

            return result;
        }

        private WebRequest CreateFtpWebRequest(string host, bool createFile = false, NetworkCredential credentials = null)
        {
#pragma warning disable SYSLIB0014 // Type or member is obsolete, see https://github.com/dotnet/docs/issues/27028
            FtpWebRequest ftpRequest;

            if (createFile)
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create($"{host}/beatpulse");

                if (credentials != null)
                {
                    ftpRequest.Credentials = credentials;
                }

                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

                using (var stream = ftpRequest.GetRequestStream())
                    stream.Write(new byte[] { 0x0 }, 0, 1);
            }
            else
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host);

                if (credentials != null)
                    ftpRequest.Credentials = credentials;

                ftpRequest.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
            }

            return ftpRequest;
#pragma warning restore SYSLIB0014 // Type or member is obsolete
        }
    }
}