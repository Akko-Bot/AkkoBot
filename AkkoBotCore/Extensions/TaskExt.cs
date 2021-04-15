using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class TaskExt
    {
        /// <summary>
        /// Executes this task asynchronously and safely returns it.
        /// </summary>
        /// <param name="task">This task.</param>
        /// <returns>This awaited task.</returns>
        public static async Task<Task> RunAndGetTaskAsync(this Task task)
        {
            try
            {
                await task;
                return task;
            }
            catch
            {
                return task;
            }
        }

        /// <summary>
        /// Executes this task asynchronously and safely returns it.
        /// </summary>
        /// <typeparam name="T">Data type held by <paramref name="task"/>.</typeparam>
        /// <param name="task">This task.</param>
        /// <returns>This awaited task.</returns>
        public static async Task<Task<T>> RunAndGetTaskAsync<T>(this Task<T> task)
        {
            try
            {
                await task;
                return task;
            }
            catch
            {
                return task;
            }
        }
    }
}
