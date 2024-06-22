using Serilog.Sinks.OpenTelemetry;

namespace Serilog.Helpers;

internal static class OpenTelemetryEnvironment
{
    public static void Configure(BatchedOpenTelemetrySinkOptions options)
    {
        options.Endpoint = Endpoint;
        options.Headers = Headers;
        options.ResourceAttributes = ResourceAttributes;
    }

    public static string Endpoint
        => Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? throw new InvalidOperationException("OTEL_EXPORTER_OTLP_ENDPOINT was not found");

    public static Dictionary<string, string> Headers
        => GetHeaders(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS"));

    public static Dictionary<string, object> ResourceAttributes
        => GetResourceAttributes(Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES"));

    private static Dictionary<string, string> GetHeaders(string? config)
    {
        Dictionary<string, string> configs = [];
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                configs.Add(parts[0], parts[1]);
            else
                throw new InvalidOperationException($"Invalid header format: {part}");
        }

        return configs;
    }

    private static Dictionary<string, object> GetResourceAttributes(string? config)
    {
        Dictionary<string, object> configs = [];
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                configs.Add(parts[0], parts[1]);
            else
                throw new InvalidOperationException($"Invalid resourceAttributes format: {part}");
        }

        return configs;
    }
}
