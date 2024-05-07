using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class CollectingExporter: IExporter
{
    public List<ExportLogsServiceRequest> Requests { get; } = new();
    
    public void Export(ExportLogsServiceRequest request)
    {
        Requests.Add(request);
    }

    public Task ExportAsync(ExportLogsServiceRequest request)
    {
        Export(request);
        return Task.CompletedTask;
    }
}