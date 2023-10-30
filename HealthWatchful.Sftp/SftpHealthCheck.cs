using Microsoft.Extensions.Diagnostics.HealthChecks;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Sftp
{
    /// <summary>
    /// A health check implementation that verifies the ability to connect to an SFTP server.
    /// </summary>
    public class SftpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _createFile;
        private readonly string _username;
        private readonly string _password;
        private readonly string _sshKeyPath;
        private readonly string _remoteFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpHealthCheck"/> class with the specified host, username, and password.
        /// </summary>
        /// <param name="host">The SFTP server host.</param>
        /// <param name="username">The username for the SFTP server.</param>
        /// <param name="password">The password for the SFTP server.</param>
        /// <param name="sshKeyPath">The optional path to the SSH private key file for authentication. Default is null.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SftpHealthCheck(string host, string username, string password, string sshKeyPath = null) : this(host, 0, username, password, sshKeyPath) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpHealthCheck"/> class with the specified host, port, username, and password.
        /// </summary>
        /// <param name="host">The SFTP server host.</param>
        /// <param name="port">The SFTP server port.</param>
        /// <param name="username">The username for the SFTP server.</param>
        /// <param name="password">The password for the SFTP server.</param>
        /// <param name="sshKeyPath">The optional path to the SSH private key file for authentication. Default is null.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SftpHealthCheck(string host, int port, string username, string password, string sshKeyPath = null) : this(host, port, username, password, sshKeyPath, false, string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpHealthCheck"/> class with the specified host, username, password, SSH key path, createFile flag, and remote file path.
        /// </summary>
        /// <param name="host">The SFTP server host.</param>
        /// <param name="username">The username for the SFTP server.</param>
        /// <param name="password">The password for the SFTP server.</param>
        /// <param name="sshKeyPath">The path to the SSH private key file for authentication.</param>
        /// <param name="createFile">A boolean flag to indicate whether a test file should be created on the server during the health check.</param>
        /// <param name="remoteFilePath">The path to the remote file on the SFTP server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SftpHealthCheck(string host, string username, string password, string sshKeyPath, bool createFile, string remoteFilePath) : this(host, 0, username, password, sshKeyPath, createFile, remoteFilePath) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpHealthCheck"/> class with the specified host, port, username, password, SSH key path, createFile flag, and remote file path.
        /// </summary>
        /// <param name="host">The SFTP server host.</param>
        /// <param name="port">The SFTP server port.</param>
        /// <param name="username">The username for the SFTP server.</param>
        /// <param name="password">The password for the SFTP server.</param>
        /// <param name="sshKeyPath">The path to the SSH private key file for authentication.</param>
        /// <param name="createFile">A boolean flag to indicate whether a test file should be created on the server during the health check.</param>
        /// <param name="remoteFilePath">The path to the remote file on the SFTP server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="host"/> is null, empty or whitespace.</exception>
        public SftpHealthCheck(string host, int port, string username, string password, string sshKeyPath, bool createFile, string remoteFilePath)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host), "Host cannot be null or whitespace!");

            if (createFile && string.IsNullOrWhiteSpace(remoteFilePath))
                throw new ArgumentNullException(nameof(remoteFilePath), "Remote File Path cannot be null, empty or whitespace when Create File option is true!");

            if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(sshKeyPath))
                throw new ArgumentNullException(nameof(sshKeyPath), "SSH Key Path cannot be empty when password is also null, empty or whitespace!");

            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _sshKeyPath = sshKeyPath;
            _createFile = createFile;
            _remoteFilePath = remoteFilePath;
        }

        /// <summary>
        /// Performs a health check by connecting to the SFTP server and optionally creating a test file.
        /// </summary>
        /// <param name="context">A context object containing information about the current health check execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with a <see cref="HealthCheckResult"/> containing the health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            var data = new Dictionary<string, object>
            {
                { "Host", _host }
            };

            if (_port > 0)
                data.Add("Port", _port);

            try
            {
                ConnectionInfo connectionInfo;

                if (_port > 0 && !string.IsNullOrWhiteSpace(_password) && string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _port, _username, new PasswordAuthenticationMethod(_username, Encoding.ASCII.GetBytes(_password)));
                else if (_port == 0 && !string.IsNullOrWhiteSpace(_password) && string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _username, new PasswordAuthenticationMethod(_username, Encoding.ASCII.GetBytes(_password)));
                else if (_port > 0 && !string.IsNullOrWhiteSpace(_password) && string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _port, _username, new PrivateKeyAuthenticationMethod(_username, new PrivateKeyFile(_sshKeyPath)));
                else if (_port > 0 && !string.IsNullOrWhiteSpace(_password) && !string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _port, _username, new PrivateKeyAuthenticationMethod(_username, new PrivateKeyFile(_sshKeyPath, _password)));
                else if (_port == 0 && !string.IsNullOrWhiteSpace(_password) && !string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _username, new PrivateKeyAuthenticationMethod(_username, new PrivateKeyFile(_sshKeyPath, _password)));
                else if (_port > 0 && string.IsNullOrWhiteSpace(_password) && !string.IsNullOrWhiteSpace(_sshKeyPath))
                    connectionInfo = new ConnectionInfo(_host, _port, _username, new PrivateKeyAuthenticationMethod(_username, new PrivateKeyFile(_sshKeyPath)));
                else
                    connectionInfo = new ConnectionInfo(_host, _username, new PrivateKeyAuthenticationMethod(_username, new PrivateKeyFile(_sshKeyPath)));

                using (var sftpClient = new SftpClient(connectionInfo))
                {
                    sftpClient.Connect();

                    bool connectionSuccess = sftpClient.IsConnected && sftpClient.ConnectionInfo.IsAuthenticated;

                    if (connectionSuccess)
                    {
                        if (_createFile)
                        {
                            using (var stream = new MemoryStream(new byte[] { 0x0 }, 0, 1))
                                sftpClient.UploadFile(stream, _remoteFilePath);
                        }

                        result = new HealthCheckResult(HealthStatus.Healthy, "OK", null, data);
                    }
                    else
                    {
                        result = new HealthCheckResult(context.Registration.FailureStatus, description: _port > 0 ? $"Connection with sftp host {_host}:{_port} failed." : $"Connection with sftp host {_host} failed.", data: data);
                    }
                }
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex, data: data);
            }

            return Task.FromResult(result);
        }
    }
}