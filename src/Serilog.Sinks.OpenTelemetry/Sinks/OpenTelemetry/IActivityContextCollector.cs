using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Collects and Provides TraceId and SpanId Information for a given <see cref="LogEvent">LogEvent</see>
/// </summary>

public interface IActivityContextCollector
{
    /// <summary>
    /// Collects TraceId and SpanId information for the given <see cref="LogEvent">LogEvent</see>
    /// </summary>
    /// <param name="logEvent">The <see cref="LogEvent">LogEvent</see> to collect TraceId and SpanId for</param>
    void CollectFor(LogEvent logEvent);

    /// <summary>
    /// Provides the previously collected TraceId and SpanId information for a given <see cref="LogEvent">LogEvent</see>
    /// </summary>
    /// <param name="logEvent">The <see cref="LogEvent">LogEvent</see> to provide TraceId and SpanId for</param>
    /// <returns>The collected TraceId and SpanId</returns>
    (string TraceId, string SpanId)? GetFor(LogEvent logEvent);
}
