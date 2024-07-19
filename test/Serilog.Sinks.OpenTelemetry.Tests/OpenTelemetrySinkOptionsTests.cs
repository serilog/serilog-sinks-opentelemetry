using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetrySinkOptionsTests
{
    [Fact]
    public void EndpointDefaultsAreCorrectForGrpc()
    {
        var opts = new OpenTelemetrySinkOptions();
        Assert.Equal(OtlpProtocol.Grpc, opts.Protocol);
        Assert.Equal("http://localhost:4317", opts.Endpoint);
        Assert.Equal("http://localhost:4317", opts.LogsEndpoint);
        Assert.Equal("http://localhost:4317", opts.TracesEndpoint);
    }
    
    [Fact]
    public void EndpointDefaultsAreCorrectForHttpProtobuf()
    {
        var opts = new OpenTelemetrySinkOptions
        {
            Protocol = OtlpProtocol.HttpProtobuf,
            Endpoint = "http://localhost:4318"
        };
        Assert.Equal(OtlpProtocol.HttpProtobuf, opts.Protocol);
        Assert.Equal("http://localhost:4318", opts.Endpoint);
        Assert.Equal("http://localhost:4318/v1/logs", opts.LogsEndpoint);
        Assert.Equal("http://localhost:4318/v1/traces", opts.TracesEndpoint);
    }

    [Fact]
    public void SignalEndpointsCanBeOverridden()
    {
        var opts = new OpenTelemetrySinkOptions
        {
            Protocol = OtlpProtocol.HttpProtobuf,
            LogsEndpoint = "http://first:4318/v1/logs",
            TracesEndpoint = "http://second:4318/v1/traces"
        };
        Assert.Equal("http://first:4318/v1/logs", opts.LogsEndpoint);
        Assert.Equal("http://second:4318/v1/traces", opts.TracesEndpoint);
    }

    [Fact]
    public void ASignalCanBeSwitchedOff()
    {
        var opts = new OpenTelemetrySinkOptions
        {
            Endpoint = null,
            Protocol = OtlpProtocol.HttpProtobuf,
            LogsEndpoint = "http://first:4318/v1/logs",
        };
        Assert.Equal("http://first:4318/v1/logs", opts.LogsEndpoint);
        Assert.Null(opts.TracesEndpoint);
    }
}
