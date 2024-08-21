using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal record struct ExportResult
    {
        private bool _isSuccess;

        internal Exception? Exception { get; private set; }

        public readonly bool IsSuccess => Exception is null && _isSuccess;

        public readonly bool IsFailure => Exception is not null || !_isSuccess;

        public static ExportResult Success() => new() { _isSuccess = true };

        public static ExportResult Failure() => new() { _isSuccess = false };

        public static Task<ExportResult> SuccessTask() => Task.FromResult(Success());

        public static Task<ExportResult> FailureTask() => Task.FromResult(Failure());

        public ExportResult WithException(Exception ex) => this with { Exception = ex };

        public void Rethrow()
        {
            if(Exception is not null)
            {
                throw Exception;
            }
        }
    }
}
