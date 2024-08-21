using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Sinks.OpenTelemetry.Exporters.ExportResults;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class CollectingExporter: IExporter
{
    public int InstrumentedRequestCount { get; private set; }
    public List<ExportLogsServiceRequest> ExportLogsServiceRequests { get; } = new();
    public List<ExportTraceServiceRequest> ExportTraceServiceRequests { get; } = new();
    
    public ExportResult Export(ExportLogsServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        ExportLogsServiceRequests.Add(request);
        return ExportResult.Success();
    }

    public Task<ExportResult> ExportAsync(ExportLogsServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        Export(request);
        return Task.FromResult(ExportResult.Success());
    }

    public ExportResult Export(ExportTraceServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        ExportTraceServiceRequests.Add(request);
        return ExportResult.Success();
    }

    public Task<ExportResult> ExportAsync(ExportTraceServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        Export(request);
        return Task.FromResult(ExportResult.Success());
    }
}
