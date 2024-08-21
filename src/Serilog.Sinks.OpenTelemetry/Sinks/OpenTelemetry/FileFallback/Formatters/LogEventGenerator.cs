using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal static class LogEventGenerator
    {
        public static LogEvent GenerateLogEvent(LogEventPropertyValue propertyValue)
        {
            return new LogEvent(DateTime.MinValue,
                LogEventLevel.Verbose,
                null,
                new MessageTemplate(Array.Empty<MessageTemplateToken>()),
                new List<LogEventProperty>() { new LogEventProperty(OtlpFormatter.OtlpMessageContents, propertyValue) });
        }
    }
}
