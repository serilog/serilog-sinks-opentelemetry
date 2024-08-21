namespace Serilog.Sinks.OpenTelemetry.FileFallback
{
    /// <summary>
    /// The support fallback log formats.
    /// </summary>
    public enum LogFormat
    {
        /// <summary>
        /// The log in Newline delimited JSON format.
        /// </summary>
        NDJson,
        /// <summary>
        /// The log in Protobuf format.
        /// </summary>
        Protobuf,
    }
}
