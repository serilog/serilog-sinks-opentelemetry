using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class StringPropertyFactory : Core.ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        return new LogEventProperty(name, new ScalarValue(value?.ToString()));
    }
}