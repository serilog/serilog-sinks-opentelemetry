using Google.Protobuf;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal class JsonToLogEvent : ILogEventFormatter
    {
        public LogEvent ToLogEvent(IMessage message) =>
            LogEventGenerator.GenerateLogEvent(new JsonLogProperty(message));
    }
}
