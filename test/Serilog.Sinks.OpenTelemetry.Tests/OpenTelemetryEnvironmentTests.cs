using Serilog.Sinks.OpenTelemetry.Configuration;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetryEnvironmentTests
{
    [Fact]
    public void ConfigureRespectsSpecificEndpointEnvironmentVariableValues()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        
        var generalEndpoint = "http://general";
        var logSpecificEndpoint = "http://log-specific";
        var tracesSpecificEndpoint = "http://traces-specific";
        
        OpenTelemetryEnvironment.Configure(options, GetEnvVar);
        
        Assert.Equal(generalEndpoint, options.Endpoint);
        Assert.Equal(logSpecificEndpoint, options.LogsEndpoint);
        Assert.Equal(tracesSpecificEndpoint, options.TracesEndpoint);
        return;

        string? GetEnvVar(string name) => name switch
        {
            OpenTelemetryEnvironmentVariables.DefaultEndpoint => generalEndpoint,
            OpenTelemetryEnvironmentVariables.LogsEndpoint => logSpecificEndpoint,
            OpenTelemetryEnvironmentVariables.TracesEndpoint => tracesSpecificEndpoint,
            _ => null
        };
    }
    
    [Fact]
    public void ConfigureFillsOptionsWithEnvironmentVariableValues()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var endpoint = "http://localhost";
        var protocol = OtlpProtocol.Grpc;
        var headers = "header1=1,header2=2";
        var resourceAttributes = "name1=1,name2=2";
        var serviceName = "my-service";

        OpenTelemetryEnvironment.Configure(options, GetEnvVar);

        Assert.Equal(endpoint, options.Endpoint);
        Assert.Equal(protocol, options.Protocol);
        Assert.Collection(options.Headers,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
        Assert.Collection(options.ResourceAttributes,
            e => Assert.Equal(("name1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("name2", "2"), (e.Key, e.Value)),
            e => Assert.Equal(("service.name", serviceName), (e.Key, e.Value)));
        return;

        string? GetEnvVar(string name) => name switch
        {
            OpenTelemetryEnvironmentVariables.DefaultEndpoint => endpoint,
            OpenTelemetryEnvironmentVariables.DefaultHeaders => headers,
            OpenTelemetryEnvironmentVariables.ResourceAttributes => resourceAttributes,
            OpenTelemetryEnvironmentVariables.DefaultProtocol => "grpc",
            OpenTelemetryEnvironmentVariables.ServiceName => serviceName,
            _ => null
        };
    }

    [Fact]
    public void ExplicitServiceNameOverridesResourceAttribute()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var resourceAttributes = "service.name=other-service";
        var serviceName = "my-service";

        OpenTelemetryEnvironment.Configure(options, GetEnvVar);

        Assert.Collection(options.ResourceAttributes,
            e => Assert.Equal(("service.name", serviceName), (e.Key, e.Value)));
        return;

        string? GetEnvVar(string name) => name switch
        {
            OpenTelemetryEnvironmentVariables.ResourceAttributes => resourceAttributes,
            OpenTelemetryEnvironmentVariables.ServiceName => serviceName,
            _ => null
        };
    }

    [Fact]
    public void ConfigureAppendPathToEndpointIfProtocolIsHttpProtobufAndEndpointDoesntEndsWithProperValue()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var endpoint = "http://localhost";
        var protocol = OtlpProtocol.HttpProtobuf;

        OpenTelemetryEnvironment.Configure(options, GetEnvVar);

        Assert.Equal(endpoint, options.Endpoint);
        Assert.Equal(protocol, options.Protocol);
        return;

        string? GetEnvVar(string name)
             => name switch
             {
                 OpenTelemetryEnvironmentVariables.DefaultEndpoint => endpoint,
                 OpenTelemetryEnvironmentVariables.DefaultProtocol => "http/protobuf",
                 _ => null
             };
    }

    [Fact]
    public void ConfigureThrowsIfHeaderEnvIsInvalidFormat()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var headers = "header1";

        var exception = Assert.Throws<ArgumentException>(() => OpenTelemetryEnvironment.Configure(options, GetEnvVar));

        Assert.StartsWith("Invalid item format `header1` in OTEL_EXPORTER_OTLP_HEADERS environment variable.", exception.Message);

        string? GetEnvVar(string name)
             => name switch
             {
                 OpenTelemetryEnvironmentVariables.DefaultHeaders => headers,
                 _ => null
             };
    }

    [Fact]
    public void ConfigureThrowsIfResourceAttributesEnvIsInvalidFormat()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var resourceAttributes = "resource1";

        var exception = Assert.Throws<ArgumentException>(() => OpenTelemetryEnvironment.Configure(options, GetEnvVar));

        Assert.StartsWith("Invalid item format `resource1` in OTEL_RESOURCE_ATTRIBUTES environment variable.", exception.Message);

        string? GetEnvVar(string name)
             => name switch
             {
                 OpenTelemetryEnvironmentVariables.ResourceAttributes => resourceAttributes,
                 _ => null
             };
    }
}
