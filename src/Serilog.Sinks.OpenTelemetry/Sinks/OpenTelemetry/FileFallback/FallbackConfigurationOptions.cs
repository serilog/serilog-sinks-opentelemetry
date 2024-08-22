namespace Serilog.Sinks.OpenTelemetry.FileFallback
{
    /// <summary>
    /// Represents the fallback configuration options.
    /// </summary>
    public class FallbackConfigurationOptions
    {
        private FileSystemFallback _logFallback = FileSystemFallback.None;
        private FileSystemFallback _traceFallback = FileSystemFallback.None;
        private FileSystemFallback _fallback = FileSystemFallback.None;

        internal FileSystemFallback LogFallback => _logFallback.IsEnabled ? _logFallback : _fallback;

        internal FileSystemFallback TraceFallback => _traceFallback.IsEnabled ? _traceFallback : _fallback;

        internal FileSystemFallback Fallback => _fallback;

        /// <summary>
        /// Sets the general fallback configuration.
        /// This fallback applies to both logs and traces if specific fallbacks (i.e., 
        /// <see cref="LogFallback"/> or <see cref="TraceFallback"/>) are not enabled.
        /// </summary>
        /// <param name="fileSinkOptions">
        /// The file sink configuration for the OpenTelemetry fallback.
        /// </param>
        /// <param name="logFormat">
        /// The format that the fallback logs will be written in.
        /// See <see cref="LogFormat"/> for available formats.
        /// </param>
        public FallbackConfigurationOptions ToFile(Action<FileSinkOptions> fileSinkOptions, LogFormat logFormat = LogFormat.NDJson)
        {
            _fallback = FileSystemFallback.Configure(fileSinkOptions, logFormat);
            return this;
        }

        /// <summary>
        /// Sets the fallback configuration for tracing.
        /// If the <see cref="TraceFallback"/> is enabled, it will be used as the fallback for traces.
        /// Otherwise, the general <see cref="Fallback"/> configuration will be used.
        /// </summary>
        /// <param name="fileSinkOptions">
        /// The file sink configuration for the OpenTelemetry fallback.
        /// </param>
        /// <param name="logFormat">
        /// The format that the fallback logs will be written in.
        /// See <see cref="LogFormat"/> for available formats.
        /// </param>
        public FallbackConfigurationOptions ToTraceFile(Action<FileSinkOptions> fileSinkOptions, LogFormat logFormat = LogFormat.NDJson)
        {
            _traceFallback = FileSystemFallback.Configure(fileSinkOptions, logFormat);
            return this;
        }

        /// <summary>
        /// Sets the fallback configuration for logging.
        /// If the <see cref="LogFallback"/> is enabled, it will be used as the fallback for logs.
        /// Otherwise, the general <see cref="Fallback"/> configuration will be used.
        /// </summary>
        /// <param name="fileSinkOptions">
        /// The file sink configuration for the OpenTelemetry fallback.
        /// </param>
        /// <param name="logFormat">
        /// The format that the fallback logs will be written in.
        /// See <see cref="LogFormat"/> for available formats.
        /// </param>
        public FallbackConfigurationOptions ToLogFile(Action<FileSinkOptions> fileSinkOptions, LogFormat logFormat = LogFormat.NDJson)
        {
            _logFallback = FileSystemFallback.Configure(fileSinkOptions, logFormat);
            return this;
        }
    }
}
