using Google.Protobuf;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal class JsonToLogEvent : ILogEventFormatter
    {
        public LogEvent ToLogEvent(IMessage message) =>
            LogEventGenerator.GenerateLogEvent(new JsonLogProperty(message));
    }
}
