using Grpc.Core;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Sinks.OpenTelemetry.Exporters;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class GrpcExporterTests
{
    [Fact]
    public async Task ExportAppliesDeadlineToCallsAgainstUnresponsiveEndpoints()
    {
        using var exporter = new GrpcExporter(
            logsEndpoint: "http://localhost:4317",
            tracesEndpoint: null,
            headers: new Dictionary<string, string>(),
            httpMessageHandler: new UnresponsiveHttpMessageHandler(),
            exportDeadline: TimeSpan.FromMilliseconds(100));

        var export = Assert.ThrowsAsync<RpcException>(() => exporter.ExportAsync(new ExportLogsServiceRequest()));

        Assert.Same(export, await Task.WhenAny(export, Task.Delay(TimeSpan.FromSeconds(30))));
        Assert.Equal(StatusCode.DeadlineExceeded, (await export).StatusCode);
    }
}
