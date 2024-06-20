using Serilog.Helpers;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetryEnvironmentVariablesTests
{
    [Fact]
    public void EndpointIsObtainedIfWhenPresent()
    {
        var endpoint = "http://localhost";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);

        var actual = OpenTelemetryEnvironmentVariables.Endpoint;

        Assert.Equal(endpoint, actual);
    }

    [Fact]
    public void EndpointThrowsExceptionIfWhenNotPresent()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironmentVariables.Endpoint);

        Assert.Equal("OTEL_EXPORTER_OTLP_ENDPOINT was not found", exception.Message);
    }

    [Fact]
    public void HeadersIsObtainedIfWhenPresent()
    {
        var headers = "header1=1,header2=2";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", headers);

        var actual = OpenTelemetryEnvironmentVariables.Headers;

        Assert.Collection(actual,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
    }

    [Fact]
    public void HeadersThrowsExceptionIfWhenNotPresent()
    {
        var headers = "header1";
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", headers);

        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironmentVariables.Headers);

        Assert.Equal("Invalid header format: header1", exception.Message);
    }

    [Fact]
    public void ResourceAttributesIsObtainedIfWhenPresent()
    {
        var headers = "header1=1,header2=2";
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", headers);

        var actual = OpenTelemetryEnvironmentVariables.ResourceAttributes;

        Assert.Collection(actual,
            e => Assert.Equal(("header1", "1"), (e.Key, e.Value)),
            e => Assert.Equal(("header2", "2"), (e.Key, e.Value)));
    }

    [Fact]
    public void ResourceAttributesThrowsExceptionIfWhenNotPresent()
    {
        var headers = "header1";
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", headers);

        var exception = Assert.Throws<InvalidOperationException>(() => OpenTelemetryEnvironmentVariables.ResourceAttributes);

        Assert.Equal("Invalid resourceAttributes format: header1", exception.Message);
    }
}
