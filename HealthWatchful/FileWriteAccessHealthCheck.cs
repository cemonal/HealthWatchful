using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful
{
    /// <summary>
    /// Represents a health check that verifies write access to a specified directory.
    /// </summary>
    public class FileWriteAccessHealthCheck : IHealthCheck
    {
        private readonly string _directoryPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWriteAccessHealthCheck"/> class.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to check for write access.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="directoryPath"/> is null, empty or whitespace.</exception>
        public FileWriteAccessHealthCheck(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath), "Directory path cannot be null or whitespace!");

            _directoryPath = directoryPath;
        }

        /// <summary>
        /// Checks the health of the system's write access to the specified directory.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation with a <see cref="HealthCheckResult"/> containing the health check status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {

                string fileName = $"test_{Guid.NewGuid()}.txt";

                // Attempt to write to the directory
                using (var stream = File.Create(Path.Combine(_directoryPath, fileName)))
                {
                    string dataasstring = "test";
                    byte[] info = new UTF8Encoding(true).GetBytes(dataasstring);
                    await stream.WriteAsync(info, 0, info.Length, cancellationToken);
                }

                // Delete the file if it was successfully written
                File.Delete(Path.Combine(_directoryPath, fileName));

                result = HealthCheckResult.Healthy("OK");
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: "The specified directory does not have write access.", exception: ex);
            }

            return result;
        }
    }
}
