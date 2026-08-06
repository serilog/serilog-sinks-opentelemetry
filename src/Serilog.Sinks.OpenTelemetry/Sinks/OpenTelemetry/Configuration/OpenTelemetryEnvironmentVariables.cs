namespace Serilog.Sinks.OpenTelemetry.Configuration;

static class OpenTelemetryEnvironmentVariables
{
    public const string DefaultEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    public const string DefaultHeaders = "OTEL_EXPORTER_OTLP_HEADERS";
    public const string DefaultProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    public const string LogsEndpoint = "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT";
 
    public const string TracesEndpoint = "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT";
 
    public const string ServiceName = "OTEL_SERVICE_NAME";
    public const string ResourceAttributes = "OTEL_RESOURCE_ATTRIBUTES";
}