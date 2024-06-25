namespace Serilog.Sinks.OpenTelemetry.Configuration;

static class OpenTelemetryEnvironment
{
    private const string PROTOCOL = "OTEL_EXPORTER_OTLP_PROTOCOL";
    private const string ENDPOINT = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string HEADERS = "OTEL_EXPORTER_OTLP_HEADERS";
    private const string RESOURCE_ATTRIBUTES = "OTEL_RESOURCE_ATTRIBUTES";

    public static void Configure(BatchedOpenTelemetrySinkOptions options, Func<string, string?> getEnvironmentVariable)
    {
        options.Protocol = getEnvironmentVariable(PROTOCOL) switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => options.Protocol
        };

        if (getEnvironmentVariable(ENDPOINT) is { Length: > 1 } endpoint)
            options.Endpoint = endpoint;

        if (options.Protocol == OtlpProtocol.HttpProtobuf && !string.IsNullOrEmpty(options.Endpoint) && !options.Endpoint.EndsWith("/v1/logs"))
            options.Endpoint = $"{options.Endpoint}/v1/logs";

        FillHeadersIfPresent(getEnvironmentVariable(HEADERS), options.Headers);

        FillHeadersResourceAttributesIfPresent(getEnvironmentVariable(RESOURCE_ATTRIBUTES), options.ResourceAttributes);
    }

    private static void FillHeadersIfPresent(string? config, IDictionary<string, string> headers)
    {
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                headers.Add(parts[0], parts[1]);
            else
                throw new InvalidOperationException($"Invalid header format: {part} in {HEADERS} environment variable.");
        }
    }

    private static void FillHeadersResourceAttributesIfPresent(string? config, IDictionary<string, object> resourceAttributes)
    {
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                resourceAttributes.Add(parts[0], parts[1]);
            else
                throw new InvalidOperationException($"Invalid resourceAttributes format: {part} in {RESOURCE_ATTRIBUTES} environment variable.");
        }
    }
}
