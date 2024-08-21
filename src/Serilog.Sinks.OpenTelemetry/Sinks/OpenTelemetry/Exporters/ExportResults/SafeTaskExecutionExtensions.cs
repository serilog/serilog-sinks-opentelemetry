namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal static class SafeTaskExecutionExtensions
    {
        /// <summary>
        /// This is implemented using a <see cref="Func{TResult}"/> instead of a raw HttpReponseMessage so we
        /// can defer execution and catch exceptions during conversion to avoid exceptions being thrown when
        /// the task is immediately fired and before the conversion is complete.
        /// </summary>
        public static async Task<Out> SafeExecutionAsync<In, Out>(this Func<Task<In>> responseTask,
            Func<In, Out> onSuccess,
            Func<Exception, Out> onError)
        {
            try
            {
                return onSuccess(await responseTask.Invoke());
            }
            catch (Exception ex)
            {
                return onError(ex);
            }
        }

        /// <summary>
        /// This is just the synchronous version of <see cref="SafeExecutionAsync"/>.
        /// </summary>
        public static Out SafeExecution<In, Out>(this Func<In> responseTask,
            Func<In, Out> onSuccess,
            Func<Exception, Out> onError)
        {
            try
            {
                return onSuccess(responseTask.Invoke());
            }
            catch (Exception ex)
            {
                return onError(ex);
            }
        }
    }
}
