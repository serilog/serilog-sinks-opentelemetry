using Google.Protobuf;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal class JsonLogProperty : LogEventPropertyValue
    {
        private JsonFormatter _formatter;
        private readonly IMessage _message;

        public JsonLogProperty(IMessage message)
        {
            _message = message;
            _formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithPreserveProtoFieldNames(true));
        }

        public override void Render(TextWriter output, string? format = null, IFormatProvider? formatProvider = null)
        {
            var jsonLog = _formatter.Format(_message);
            output.WriteLine(jsonLog);
        }
    }
}
