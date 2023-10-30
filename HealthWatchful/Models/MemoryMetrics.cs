namespace HealthWatchful.Models
{
    /// <summary>
    /// Represents memory metrics including total, used, and free memory.
    /// </summary>
    internal class MemoryMetrics
    {
        /// <summary>
        /// Gets or sets the total amount of memory in megabytes.
        /// </summary>
        public double Total;

        /// <summary>
        /// Gets or sets the used amount of memory in megabytes.
        /// </summary>
        public double Used;

        /// <summary>
        /// Gets or sets the free amount of memory in megabytes.
        /// </summary>
        public double Free;
    }
}