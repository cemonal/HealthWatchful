using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Services
{
    /// <summary>
    /// Provides a service for performing health checks on registered health check implementations.
    /// </summary>
    public class HealthCheckService : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<HealthCheckServiceOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options for configuring the health check service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is null.</exception>
        public HealthCheckService(IOptions<HealthCheckServiceOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options), "Options cannot be null!");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class with the specified service scope factory
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="options">The options for configuring the health check service.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="scopeFactory"/> is null.</exception>
        public HealthCheckService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory), "Scope factory cannot be null!");

            using (var scope = _scopeFactory.CreateAsyncScope())
                _options = scope.ServiceProvider.GetService<IOptions<HealthCheckServiceOptions>>() ?? throw new ArgumentNullException("HealthCheckServiceOptions cannot be null!");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class with the specified health checks.
        /// </summary>
        /// <param name="healthChecks">The health checks to be registered with the service.</param>
        public HealthCheckService(IReadOnlyDictionary<string, IHealthCheck> healthChecks)
        {
            var options = new HealthCheckServiceOptions();

            foreach (var x in healthChecks)
            {
                options.Registrations.Add(new HealthCheckRegistration(x.Key, x.Value, HealthStatus.Unhealthy, null));
            }

            _options = Options.Create(options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class with the specified health check contexts.
        /// </summary>
        /// <param name="healthChecks">The health check contexts to be registered with the service.</param>
        public HealthCheckService(IEnumerable<HealthCheckContext> healthChecks)
        {
            var options = new HealthCheckServiceOptions();

            foreach (var x in healthChecks)
            {
                options.Registrations.Add(x.Registration);
            }

            _options = Options.Create(options);
        }

        /// <summary>
        /// Runs the registered health checks and returns a <see cref="HealthReport"/>.
        /// </summary>
        /// <param name="predicate">A function that filters which health checks to run. If null, all health checks are run.</param>
        /// <param name="cancellationToken">An optional CancellationToken to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with a <see cref="HealthReport"/> containing the health check result.</returns>
        public async override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (_scopeFactory != null)
            {
                using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var registrations = _options.Value.Registrations.Where(x => predicate == null || predicate(x));
                    return await RunAsync(scope.ServiceProvider, registrations, cancellationToken);
                }
            }
            else
            {
                var registrations = _options.Value.Registrations.Where(x => predicate == null || predicate(x));
                return await RunAsync(null, registrations, cancellationToken);
            }
        }

        private async Task<HealthReport> RunAsync(IServiceProvider serviceProvider, IEnumerable<HealthCheckRegistration> registrations, CancellationToken cancellationToken = default)
        {
            var entries = new ConcurrentDictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase);
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>(registrations.Count());

            foreach (var registration in registrations)
            {
                var task = Task.Run(async () =>
                {
                    var hcSw = Stopwatch.StartNew();
                    var ct = cancellationToken;
                    CancellationTokenSource cts = null;

                    if (registration.Timeout > TimeSpan.Zero)
                    {
                        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(registration.Timeout);
                        ct = cts.Token;
                    }

                    try
                    {
                        HealthCheckResult result;

                        var context = new HealthCheckContext { Registration = registration };

                        result = await registration.Factory(serviceProvider).CheckHealthAsync(context, ct).ConfigureAwait(false);
                        entries.TryAdd(registration.Name, new HealthReportEntry(result.Status, result.Description, hcSw.Elapsed, result.Exception, result.Data, registration.Tags));
                    }
                    catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
                    {
                        entries.TryAdd(registration.Name, new HealthReportEntry(registration.FailureStatus, "A timeout occurred while running check.", hcSw.Elapsed, ex, null, registration.Tags));
                    }
                    catch (Exception ex)
                    {
                        entries.TryAdd(registration.Name, new HealthReportEntry(registration.FailureStatus, ex.Message, hcSw.Elapsed, ex, null, registration.Tags));
                    }
                    finally
                    {
                        cts?.Dispose();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return new HealthReport(entries, stopwatch.Elapsed);
        }
    }
}