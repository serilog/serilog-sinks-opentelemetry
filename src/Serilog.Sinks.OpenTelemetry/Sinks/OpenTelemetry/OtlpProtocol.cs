namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Defines the OTLP protocol to use when sending OpenTelemetry data.
/// </summary>
public enum OtlpProtocol
{
    /// <summary>
    /// Sends OpenTelemetry data encoded as a protobuf message over gRPC.
    /// </summary>
    GrpcProtobuf,

    /// <summary>
    /// Posts OpenTelemetry data encoded as a protobuf message over HTTP.
    /// </summary>
    HttpProtobuf
}