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
        var sink = new CollectingSink();
        var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        emitter(logger);
        return Assert.Single(sink._emitted);
    }
}
