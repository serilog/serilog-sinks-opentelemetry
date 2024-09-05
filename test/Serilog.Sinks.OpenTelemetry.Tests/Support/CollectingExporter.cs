using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class CollectingExporter: IExporter
{
    public int InstrumentedRequestCount { get; private set; }
    public List<ExportLogsServiceRequest> ExportLogsServiceRequests { get; } = new();
    public List<ExportTraceServiceRequest> ExportTraceServiceRequests { get; } = new();
    
    public void Export(ExportLogsServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        ExportLogsServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportLogsServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        Export(request);
        return Task.CompletedTask;
    }

    public void Export(ExportTraceServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        ExportTraceServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportTraceServiceRequest request)
    {
        if (!TestSuppressInstrumentationScope.IsSuppressed) InstrumentedRequestCount++;
        Export(request);
        return Task.CompletedTask;
    }
}
