using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Sinks.OpenTelemetry.Exporters;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class InstrumentationSuppressingExporterTests
{
    [Fact]
    public void RequestsAreNotInstrumentedWhenSuppressed()
    {
        var exporter = new CollectingExporter();
        
        exporter.Export(new ExportLogsServiceRequest());
        Assert.Equal(1, exporter.InstrumentedRequestCount);
        Assert.Single(exporter.ExportLogsServiceRequests);

        var wrapper = new InstrumentationSuppressingExporter(exporter, TestSuppressInstrumentationScope.Begin);
        wrapper.Export(new ExportLogsServiceRequest());
        Assert.Equal(1, exporter.InstrumentedRequestCount);
        Assert.Equal(2, exporter.ExportLogsServiceRequests.Count);
    }
}
