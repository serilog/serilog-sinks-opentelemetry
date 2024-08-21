using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Sinks.OpenTelemetry.Exporters.ExportResults;
using System.Runtime.ExceptionServices;
using static Serilog.Sinks.OpenTelemetry.Exporters.ExportResults.ExportResultExtensions;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class FailingExporter: IExporter
{
    public List<ExportLogsServiceRequest> ExportLogsServiceRequests { get; } = new();
    public List<ExportTraceServiceRequest> ExportTraceServiceRequests { get; } = new();
    private Exception? _ex;

    public FailingExporter(Exception? ex = null)
    {
        _ex = ex;
    }

    private bool ThrowIfSet()
    {
        if (_ex is null) return false;
        throw _ex;
    }

    private SuccessEvaluator<bool> Default => (bool b) => false;

    public ExportResult Export(ExportLogsServiceRequest request)
    {
        ExportLogsServiceRequests.Add(request);
        var action = () => ThrowIfSet();
        return action.ToExportResult(Default);
    }

    public Task<ExportResult> ExportAsync(ExportLogsServiceRequest request)
    {
        ExportLogsServiceRequests.Add(request);
        var action = () => Task.FromResult(ThrowIfSet());
        return action.ToExportResult(Default);
    }

    public ExportResult Export(ExportTraceServiceRequest request)
    {
        ExportTraceServiceRequests.Add(request);
        var action = () => ThrowIfSet();
        return action.ToExportResult(Default);
    }

    public Task<ExportResult> ExportAsync(ExportTraceServiceRequest request)
    {
        ExportTraceServiceRequests.Add(request);
        var action = () => Task.FromResult(ThrowIfSet());
        return action.ToExportResult(Default);
    }
}
