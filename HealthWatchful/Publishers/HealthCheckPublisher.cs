using HealthWatchful.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WebhookSenderHub;

namespace HealthWatchful.Publishers
{
    /// <summary>
    /// A health check publisher implementation that sends notifications to a webhook service.
    /// </summary>
    public class HealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly string _serviceName;
        private readonly IWebhookService<IMessageCard>[] _webhookServices;
        private bool _healty = true;
        private readonly ILogger<HealthCheckPublisher> _logger;

        public HealthCheckPublisher(IWebhookService<IMessageCard>[] webhookServices, string serviceName = null) : this(webhookServices, null, serviceName) { }

        public HealthCheckPublisher(IWebhookService<IMessageCard>[] webhookServices, ILogger<HealthCheckPublisher> logger, string serviceName = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                var hostName = Dns.GetHostName();
                var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                serviceName = $"[{(string.IsNullOrEmpty(hostName) ? Environment.MachineName : hostName)}] {appName}";
            }

            _logger = logger;
            _webhookServices = webhookServices;
            _serviceName = serviceName;
        }

        /// <summary>
        /// Publishes the health report to the webhook service.
        /// </summary>
        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var message = report.ToMessage().Replace("[[LIVENESS]]", _serviceName);
            var tasks = new List<Task>();

            if (report.Status == HealthStatus.Healthy && !_healty)
            {
                _healty = true;

                _logger?.LogInformation($"[HealthChecks] {_serviceName} has recovered!");

                if (_webhookServices != null && _webhookServices.Any())
                {
                    foreach (var webhookService in _webhookServices)
                    {
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                bool response = false;

                                if (webhookService is IMessagingService<IMessageCard> service)
                                    response = await service.SendAsync(message, $"{_serviceName} has recovered!", $"{_serviceName} has recovered!", "00CC00", cancellationToken).ConfigureAwait(false);
                                else
                                    response = await webhookService.SendAsync(message, cancellationToken).ConfigureAwait(false);

                                if (!response)
                                    _logger?.LogError("[HealthChecks] Unable to publish health report!");
                                else
                                    _logger?.LogInformation("[HealthChecks] Health report successfully published!");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "[HealthChecks] Unable to publish health report!");
                            }

                        }, cancellationToken);

                        tasks.Add(task);
                    }
                }
            }
            else if (report.Status == HealthStatus.Unhealthy)
            {
                if (_healty)
                    _healty = false;

                if (_logger != null)
                {
                    _logger.LogError($"[HealthChecks] {_serviceName} has failed!{Environment.NewLine}Message: {message}{Environment.NewLine}Total Duration: {report.TotalDuration}");

                    report.Entries.Where(x => x.Value.Status == HealthStatus.Unhealthy && x.Value.Exception != null).ToList().ForEach(x =>
                    {
                        _logger.LogError(x.Value.Exception, x.Key + " health check got an error: " + x.Value.Exception.Message);
                    });
                }

                if (_webhookServices != null && _webhookServices.Any())
                {
                    foreach (var webhookService in _webhookServices)
                    {
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                bool response = false;

                                if (webhookService is IMessagingService<IMessageCard> service)
                                    response = await service.SendAsync(message, $"{_serviceName} has failed!", $"{_serviceName} has failed!", "FF3333", cancellationToken).ConfigureAwait(false);
                                else
                                    response = await webhookService.SendAsync(message, cancellationToken).ConfigureAwait(false);

                                if (!response)
                                    _logger?.LogError("[HealthChecks] Unable to publish health report!");
                                else
                                    _logger?.LogInformation("[HealthChecks] Health report successfully published!");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "[HealthChecks] Unable to publish health report!");
                            }

                        }, cancellationToken);

                        tasks.Add(task);
                    }
                }
            }

            if (tasks.Any())
                await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}