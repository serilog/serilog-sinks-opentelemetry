using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class CollectingExporter: IExporter
{
    public List<ExportLogsServiceRequest> ExportLogsServiceRequests { get; } = new();
    public List<ExportTraceServiceRequest> ExportTraceServiceRequests { get; } = new();
    
    public void Export(ExportLogsServiceRequest request)
    {
        ExportLogsServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportLogsServiceRequest request)
    {
        Export(request);
        return Task.CompletedTask;
    }

    public void Export(ExportTraceServiceRequest request)
    {
        ExportTraceServiceRequests.Add(request);
    }

    public Task ExportAsync(ExportTraceServiceRequest request)
    {
        Export(request);
        return Task.CompletedTask;
    }
}