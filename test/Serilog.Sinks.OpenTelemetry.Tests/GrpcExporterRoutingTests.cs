using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Sinks.OpenTelemetry.Exporters;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class GrpcExporterRoutingTests
{
    [Fact]
    public void TracesOnlyConfigurationCanBeConstructed()
    {
        using var exporter = new GrpcExporter(
            logsEndpoint: null,
            tracesEndpoint: "http://localhost:4317",
            headers: new Dictionary<string, string>());

        Assert.NotNull(exporter);
    }

    [Fact]
    public async Task TraceExportsTargetTheTracesEndpoint()
    {
        var handler = new RecordingHttpMessageHandler();
        using var exporter = new GrpcExporter(
            logsEndpoint: "http://logs.example.com",
            tracesEndpoint: "http://traces.example.com",
            headers: new Dictionary<string, string>(),
            httpMessageHandler: handler);

        await Assert.ThrowsAsync<RpcException>(() => exporter.ExportAsync(new ExportTraceServiceRequest()));

        var uri = Assert.Single(handler.RequestUris);
        Assert.Equal("traces.example.com", uri?.Host);
    }
}
