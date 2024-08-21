using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Serilog.Sinks.OpenTelemetry.Exporters.ExportResults.ExportResultExtensions;

namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal static class ExportResultExtensions
    {
        public static SuccessEvaluator<ExportTraceServiceResponse> TraceSuccessEvaluator =>
            (response) => response.PartialSuccess.RejectedSpans == 0;

        public static SuccessEvaluator<ExportLogsServiceResponse> LogSuccessEvaluator =>
           (response) => response.PartialSuccess.RejectedLogRecords == 0;

        public static SuccessEvaluator<HttpResponseMessage> HttpSuccessEvaluator =>
            (reponse) => reponse.IsSuccessStatusCode;

        public delegate bool SuccessEvaluator<T>(T response);

        private static Func<bool> Evaluate<T>(SuccessEvaluator<T> evaluator, T response) => () => evaluator(response);

        public static ExportResult ToExportResult(this Func<bool> response)
            => response()
            ? ExportResult.Success()
            : ExportResult.Failure();

        public static ExportResult ToExportResult<T>(this Func<T> response, SuccessEvaluator<T> successEvaluator)
             => response.SafeExecution(
                 onSuccess: r => Evaluate(successEvaluator, r).ToExportResult(),
                 onError: (ex) => ExportResult.Failure().WithException(ex));

        public static Task<ExportResult> ToExportResult<T>(this Func<Task<T>> response, SuccessEvaluator<T> successEvaluator)
            => response.SafeExecutionAsync(
                 onSuccess: r => Evaluate(successEvaluator, r).ToExportResult(),
                 onError: ex => ExportResult.Failure().WithException(ex));
    }
}
