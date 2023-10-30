using HealthWatchful.Extensions;
using HealthWatchful.Models;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Services
{
    /// <summary>
    /// A service that retrieves memory usage metrics.
    /// </summary>
    internal class MemoryMetricsService
    {
        private readonly bool _useShellExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMetricsService"/> class with the <paramref name="useShellExecute"/> value set to true.
        /// </summary>
        public MemoryMetricsService() : this(true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryMetricsService"/> class.
        /// </summary>
        /// <param name="useShellExecute">A value indicating whether to use the operating system shell when starting a process.</param>
        public MemoryMetricsService(bool useShellExecute)
        {
            _useShellExecute = useShellExecute;
        }

        /// <summary>
        /// Gets the memory usage metrics asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns the <see cref="MemoryMetrics"/> object containing the memory usage metrics.</returns>
        public async Task<MemoryMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
        {
            if (IsUnix())
                return await GetUnixMetricsAsync(cancellationToken).ConfigureAwait(false);

            return await GetWindowsMetricsAsync(cancellationToken).ConfigureAwait(false);
        }

        private bool IsUnix()
        {
            var isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            return isUnix;
        }

        private async Task<MemoryMetrics> GetWindowsMetricsAsync(CancellationToken cancellationToken)
        {
            var output = "";

            var info = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value",
                RedirectStandardOutput = true,
                UseShellExecute = _useShellExecute
            };

            using (var process = Process.Start(info))
            {
                output = await process.StandardOutput.ReadToEndAsync().WithCancellationTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            var lines = output.Trim().Split(new[] { "\n" }, StringSplitOptions.None);
            var freeMemoryParts = lines[0].Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics
            {
                Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0),
                Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0)
            };

            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }

        private async Task<MemoryMetrics> GetUnixMetricsAsync(CancellationToken cancellationToken)
        {
            var output = "";

            var info = new ProcessStartInfo("free -m")
            {
                FileName = "/bin/bash",
                Arguments = "-c \"free -m\"",
                RedirectStandardOutput = true,
                UseShellExecute = _useShellExecute
            };

            using (var process = Process.Start(info))
            {
                output = await process.StandardOutput.ReadToEndAsync().WithCancellationTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            var lines = output.Split(new[] { "\n" }, StringSplitOptions.None);
            var memory = lines[1].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics
            {
                Total = double.Parse(memory[1]),
                Used = double.Parse(memory[2]),
                Free = double.Parse(memory[3])
            };

            return metrics;
        }
    }
}