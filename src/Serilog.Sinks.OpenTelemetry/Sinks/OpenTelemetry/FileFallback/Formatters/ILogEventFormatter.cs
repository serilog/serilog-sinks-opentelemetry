using Google.Protobuf;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal interface ILogEventFormatter
    {
        public LogEvent ToLogEvent(IMessage message);
    }
}
