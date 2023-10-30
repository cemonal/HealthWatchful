using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Text;

namespace HealthWatchful.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="HealthReport"/> objects to support message formatting.
    /// </summary>
    internal static class HealthReportExtensions
    {
        /// <summary>
        /// Converts the <see cref="HealthReport"/> object to a formatted message string that summarizes its state.
        /// </summary>
        /// <param name="report">The <see cref="HealthReport"/> object to be converted.</param>
        /// <returns>A formatted message string representing the <see cref="HealthReport"/> state.</returns>
        public static string ToMessage(this HealthReport report)
        {
            if (report.Status == HealthStatus.Healthy)
                return $"The HealthCheck [[LIVENESS]] is recovered. All is up and running";

            var sb = new StringBuilder();

            var unhealthy = report.Entries.Where(x => x.Value.Status == HealthStatus.Unhealthy);

            sb.AppendLine($"There are at least **{unhealthy.Count()}** healthcheck(s) failing:");
            sb.AppendLine();

            foreach (var item in unhealthy)
            {
                sb.Append($"- **{item.Key}:** {item.Value.Description} \r");
            }

            return sb.ToString();
        }
    }
}