using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class CollectingSink: ILogEventSink
{
    readonly List<LogEvent> _emitted = new();

    public void Emit(LogEvent logEvent)
    {
        _emitted.Add(logEvent);
    }

    public static LogEvent CollectSingle(Action<ILogger> emitter)
    {
        return Assert.Single(Collect(emitter));
    }

    public static IReadOnlyList<LogEvent> Collect(Action<ILogger> emitter)
    {
        var sink = new CollectingSink();
        var collector = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .Destructure.AsScalar<ActivityTraceId>()
            .Destructure.AsScalar<ActivitySpanId>()
            .CreateLogger();
        
        emitter(collector);

        return sink._emitted;
    }
}
