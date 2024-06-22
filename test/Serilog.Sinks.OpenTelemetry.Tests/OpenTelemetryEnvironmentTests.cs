using Serilog.Helpers;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetryEnvironmentTests
{
    [Fact]
    public void ConfigureFillOptionsWithEnvironmentVariablesValues()
    {
        BatchedOpenTelemetrySinkOptions options = new();
        var endpoint = "http://localhost";
        var headers = "header1=1,header2=2";
        var resourceAttributes = "name1=1,name2=2";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", headers);
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", resourceAttributes);

        OpenTelemetryEnvironment.Configure(options);

        Assert.Equal(endpoint, options.Endpoint);
        Assert.Collection(options.Headers,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
        Assert.Collection(options.ResourceAttributes,
            e => Assert.Equal(("name1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("name2", "2"), (e.Key, e.Value)));
    }

    [Fact]
    public void EndpointIsObtainedIfWhenPresent()
    {
        var endpoint = "http://localhost";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);

        var actual = OpenTelemetryEnvironment.Endpoint;

        Assert.Equal(endpoint, actual);
    }

    [Fact]
    public void EndpointThrowsExceptionIfWhenNotPresent()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironment.Endpoint);

        Assert.Equal("OTEL_EXPORTER_OTLP_ENDPOINT was not found", exception.Message);
    }

    [Fact]
    public void HeadersIsObtainedIfWhenPresent()
    {
        var headers = "header1=1,header2=2";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", headers);

        var actual = OpenTelemetryEnvironment.Headers;

        Assert.Collection(actual,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
    }

    [Fact]
    public void HeadersThrowsExceptionIfWhenNotPresent()
    {
        var headers = "header1";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", headers);

        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironment.Headers);

        Assert.Equal("Invalid header format: header1", exception.Message);
    }

    [Fact]
    public void ResourceAttributesIsObtainedIfWhenPresent()
    {
        var headers = "header1=1,header2=2";
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", headers);

        var actual = OpenTelemetryEnvironment.ResourceAttributes;

        Assert.Collection(actual,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
    }

    [Fact]
    public void ResourceAttributesThrowsExceptionIfWhenNotPresent()
    {
        var headers = "header1";
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", headers);

        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironment.ResourceAttributes);

        Assert.Equal("Invalid resourceAttributes format: header1", exception.Message);
    }
}
