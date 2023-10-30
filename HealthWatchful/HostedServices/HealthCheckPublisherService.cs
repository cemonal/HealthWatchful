using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.HostedServices
{
    /// <summary>
    /// A hosted service for publishing health check results. This service periodically
    /// runs the registered health checks and publishes the results through the registered
    /// health check publishers. The frequency of health check execution and the timeout
    /// for the health check execution are configurable.
    /// </summary>
    public class HealthCheckPublisherService : IHostedService
    {
        private readonly IOptions<HealthCheckPublisherOptions> _options;
        private Timer _timer;
        private readonly HealthCheckService _healthCheckService;
        private readonly IHealthCheckPublisher[] _publishers;
        private readonly CancellationTokenSource _stopping;
        private readonly ILogger<HealthCheckPublisherService> _logger;

        /// <summary>
        /// Creates an instance of the <see cref="HealthCheckPublisherService"/>. This constructor should be used when a logger is available.
        /// </summary>
        /// <param name="options">The options for the <see cref="HealthCheckPublisherService"/>, such as frequency of health check execution and timeout for health check execution.</param>
        /// <param name="healthCheckService">The health check service used to execute the health checks.</param>
        /// <param name="publishers">The health check publishers used to publish the results of the health checks.</param>
        /// <param name="logger">The logger used to log information and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/>, <paramref name="healthCheckService"/>, or <paramref name="publishers"/> is null.</exception>
        public HealthCheckPublisherService(IOptions<HealthCheckPublisherOptions> options, HealthCheckService healthCheckService, IHealthCheckPublisher[] publishers, ILogger<HealthCheckPublisherService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options), "Options cannot be null!");
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService), "Please define the HealthCheck service!");
            _publishers = publishers ?? throw new ArgumentNullException(nameof(publishers), "Please define the HealthCheck Publishers!");
            _logger = logger;
            _stopping = new CancellationTokenSource();
        }

        /// <summary>
        /// Creates an instance of the <see cref="HealthCheckPublisherService"/> without a logger.
        /// </summary>
        /// <param name="options">The options for the <see cref="HealthCheckPublisherService"/>, such as frequency of health check execution and timeout for health check execution.</param>
        /// <param name="healthCheckService">The health check service used to execute the health checks.</param>
        /// <param name="publishers">The health check publishers used to publish the results of the health checks.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="healthCheckService"/> or <paramref name="publishers"/> is null.</exception>
        public HealthCheckPublisherService(IOptions<HealthCheckPublisherOptions> options, HealthCheckService healthCheckService, IHealthCheckPublisher[] publishers) : this(options, healthCheckService, publishers, null) { }

        internal bool IsStopping => _stopping.IsCancellationRequested;

        internal bool IsTimerRunning => _timer != null;

        private async void OnTimedEvent(object state)
        {
            CancellationTokenSource cts = null;

            try
            {
                _logger?.LogDebug("Health check triggered!");

                var tasks = new List<Task>(_publishers.Length);
                var period = _options.Value.Period.TotalMilliseconds;

                cts = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                cts.CancelAfter(_options.Value.Timeout);

                var report = await _healthCheckService.CheckHealthAsync(_options.Value.Predicate, cts.Token).ConfigureAwait(false);

                foreach (var publisher in _publishers)
                {
                    tasks.Add(publisher.PublishAsync(report, cts.Token));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
                // a timeout and we want to log it.
            }
            catch (TaskCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it. Otherwise, it's
                // a timeout and we want to log it.
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Health check publisher hosted service failed!");
            }
            finally
            {
                cts?.Dispose();

                if (!IsStopping)
                {
                    _timer?.Change(_options.Value.Period, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Starts the <see cref="HealthCheckPublisherService"/>. The service will start executing the registered health checks and publishing the results through the registered health check publishers.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_publishers.Any())
                return Task.CompletedTask;

            _logger?.LogInformation("Health check publisher hosted service started!");

            _timer = new Timer(OnTimedEvent, null, _options.Value.Delay, Timeout.InfiniteTimeSpan);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the <see cref="HealthCheckPublisherService"/>. The service will stop executing health checks and publishing results.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Health check publisher hosted service stopped!");
                _stopping.Cancel();
            }
            catch
            {
                // Ignore exceptions thrown as a result of a cancellation.
            }

            if (!_publishers.Any())
                return Task.CompletedTask;

            _timer?.Dispose();
            _timer = null;

            return Task.CompletedTask;
        }
    }
}