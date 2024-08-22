using Serilog.Configuration;
using Serilog.Sinks.File;
using Serilog.Sinks.OpenTelemetry.FileFallback.Formatters;

namespace Serilog.Sinks.OpenTelemetry.FileFallback
{
    /// <summary>
    /// The options for the <see cref="FileSink"/> when fallback is enabled.
    /// </summary>
    public class FileSinkOptions
    {
        /// <summary>
        /// The path to the log file.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The maximum size, in bytes, to which the log file can grow before rolling over.
        /// Default is unlimited file growth.
        /// </summary>
        public long? FileSizeLimitBytes { get; set; }

        /// <summary>
        /// Whether events should be buffered before being written to disk.
        /// </summary>
        public bool Buffered { get; set; } = false;

        /// <summary>
        /// Share the log file with other processes.
        /// </summary>
        public bool Shared { get; set; } = false;

        /// <summary>
        /// How often the buffer should be flushed to disk.
        /// </summary>
        public TimeSpan? FlushToDiskInterval { get; set; }

        /// <summary>
        /// The interval at which log files should roll over.
        /// </summary>
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Infinite;

        /// <summary>
        /// Whether the log file should be rolled when it reaches the file size limit.
        /// </summary>
        public bool RollOnFileSizeLimit { get; set; } = false;

        /// <summary>
        /// The maximum number of log files that will be retained, including the current log file.
        /// Default is unlimited retention.
        /// </summary>
        public int? RetainedFileCountLimit { get; set; }

        /// <summary>
        /// Hooks for controlling file lifecycle events.
        /// </summary>
        public FileLifecycleHooks? Hooks { get; set; }

        /// <summary>
        /// The time limit to retain log files.
        /// </summary>
        public TimeSpan? RetainedFileTimeLimit { get; set; }
    }

    internal static class FileSinkOptionsExtensions
    {
        public static LoggerConfiguration FallbackFile(this LoggerSinkConfiguration wt, FileSinkOptions optionsWithFallback)
            => wt.File(
                path: optionsWithFallback.Path,
                buffered: optionsWithFallback.Buffered,
                shared: optionsWithFallback.Shared,
                fileSizeLimitBytes: optionsWithFallback.FileSizeLimitBytes,
                flushToDiskInterval: optionsWithFallback.FlushToDiskInterval,
                rollingInterval: optionsWithFallback.RollingInterval,
                rollOnFileSizeLimit: optionsWithFallback.RollOnFileSizeLimit,
                retainedFileCountLimit: optionsWithFallback.RetainedFileCountLimit,
                retainedFileTimeLimit: optionsWithFallback.RetainedFileTimeLimit,
                hooks: optionsWithFallback.Hooks,
                formatter: new OtlpFormatter()
                );
    }
}

