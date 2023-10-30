using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Elasticsearch
{
    /// <summary>
    /// Represents a health check that monitors the availability and connectivity of an Elasticsearch server.
    /// </summary>
    public class ElasticsearchHealthCheck : IHealthCheck
    {
        private static readonly ConcurrentDictionary<string, ElasticClient> _connections = new ConcurrentDictionary<string, ElasticClient>();

        private readonly ElasticsearchOptions _options;

        /// <summary>
        /// Initializes a new instance of the ElasticsearchHealthCheck class with the specified options.
        /// </summary>
        /// <param name="options">An instance of ElasticsearchOptions containing configuration settings.</param>
        public ElasticsearchHealthCheck(ElasticsearchOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        /// <summary>
        /// Checks the health of the Elasticsearch server by monitoring its availability and connectivity.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connections.TryGetValue(_options.Uri, out var lowLevelClient))
                {
                    var settings = new ConnectionSettings(new Uri(_options.Uri));

                    if (_options.RequestTimeout.HasValue)
                    {
                        settings = settings.RequestTimeout(_options.RequestTimeout.Value);
                    }

                    if (_options.AuthenticateWithBasicCredentials)
                    {
                        settings = settings.BasicAuthentication(_options.UserName, _options.Password);
                    }
                    else if (_options.AuthenticateWithCertificate)
                    {
                        settings = settings.ClientCertificate(_options.Certificate);
                    }
                    else if (_options.AuthenticateWithApiKey)
                    {
                        settings = settings.ApiKeyAuthentication(_options.ApiKeyAuthenticationCredentials);
                    }

                    if (_options.CertificateValidationCallback != null)
                    {
                        settings = settings.ServerCertificateValidationCallback(_options.CertificateValidationCallback);
                    }

                    lowLevelClient = new ElasticClient(settings);

                    if (!_connections.TryAdd(_options.Uri, lowLevelClient))
                    {
                        lowLevelClient = _connections[_options.Uri];
                    }
                }

                if (_options.UseClusterHealthApi)
                {
                    var healthResponse = await lowLevelClient.Cluster.HealthAsync(ct: cancellationToken).ConfigureAwait(false);

                    if (healthResponse.ApiCall.HttpStatusCode != 200)
                        return new HealthCheckResult(context.Registration.FailureStatus);

                    switch (healthResponse.Status)
                    {
                        case global::Elasticsearch.Net.Health.Green:
                            return HealthCheckResult.Healthy("OK");
                        case global::Elasticsearch.Net.Health.Yellow:
                            return HealthCheckResult.Degraded();
                        case global::Elasticsearch.Net.Health.Red:
                        default:
                            return new HealthCheckResult(context.Registration.FailureStatus);
                    }
                }

                var pingResult = await lowLevelClient.PingAsync(ct: cancellationToken).ConfigureAwait(false);
                bool isSuccess = pingResult.ApiCall.HttpStatusCode == 200;

                return isSuccess
                    ? HealthCheckResult.Healthy()
                    : new HealthCheckResult(context.Registration.FailureStatus);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}