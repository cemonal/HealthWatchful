using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.Extensions
{
    /// <summary>
    /// Provides extension methods for tasks to support cancellation tokens.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Executes the task with a specified CancellationToken.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="cancellationToken">The CancellationToken to be used for the task.</param>
        /// <returns>A task representing the async operation.</returns>
        public static async Task WithCancellationTokenAsync(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            cancellationToken.Register(() => tcs.SetResult(true));

            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
            {
                throw new OperationCanceledException("The operation has timed out", cancellationToken);
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the task with a specified CancellationToken and returns the result.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="task">The task to be executed.</param>
        /// <param name="cancellationToken">The CancellationToken to be used for the task.</param>
        /// <returns>A task representing the async operation with the result of type T.</returns>
        public static async Task<T> WithCancellationTokenAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();

            cancellationToken.Register(() => tcs.SetResult(default));

            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                throw new OperationCanceledException("The operation has timed out", cancellationToken);

            return await task.ConfigureAwait(false);
        }
    }
}
