using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal static class GrpcExportResultExtensions
    {
        private static ExportResult ToExportResult(this ExportLogsServiceResponse response)
            => response.PartialSuccess.RejectedLogRecords == 0
            ? ExportResult.Success()
            : ExportResult.Failure();

        private static ExportResult ToExportResult(this ExportTraceServiceResponse response)
            => response.PartialSuccess.RejectedSpans == 0
            ? ExportResult.Success()
            : ExportResult.Failure();

        public static ExportResult ToExportResult(this Func<ExportLogsServiceResponse> response)
            => response.SafeExecution(
                onSuccess: r => r.ToExportResult(),
                onError: (ex) => ExportResult.Failure().WithException(ex));

        public static ExportResult ToExportResult(this Func<ExportTraceServiceResponse> response)
            => response.SafeExecution(
                onSuccess: r => r.ToExportResult(),
                onError: (ex) => ExportResult.Failure().WithException(ex));

        public static Task<ExportResult> ToExportResult(this Func<Task<ExportLogsServiceResponse>> response)
            => response.SafeExecutionAsync(
                onSuccess: ToExportResult,
                onError: ex => ExportResult.Failure().WithException(ex));

        public static Task<ExportResult> ToExportResult(this Func<Task<ExportTraceServiceResponse>> response)
            => response.SafeExecutionAsync(
                onSuccess: ToExportResult,
                onError: ex => ExportResult.Failure().WithException(ex));
    }
}
