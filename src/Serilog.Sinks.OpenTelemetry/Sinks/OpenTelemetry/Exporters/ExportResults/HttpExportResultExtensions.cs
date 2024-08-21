namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal static class HttpExportResultExtensions
    {
        private static ExportResult ToExportResult(this HttpResponseMessage response)
            => response.IsSuccessStatusCode
            ? ExportResult.Success()
            : ExportResult.Failure();

        public static ExportResult ToExportResult(this Func<HttpResponseMessage> response)
            => response.SafeExecution(
                onSuccess: r => r.ToExportResult(),
                onError: (ex) => ExportResult.Failure().WithException(ex));

        public static Task<ExportResult> ToExportResult(this Func<Task<HttpResponseMessage>> response)
            => response.SafeExecutionAsync(
                onSuccess: ToExportResult,
                onError: ex => ExportResult.Failure().WithException(ex));
    }
}
