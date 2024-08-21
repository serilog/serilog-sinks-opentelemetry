using Google.Protobuf;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal class ProtobufLogProperty : LogEventPropertyValue
    {
        private readonly IMessage _message;

        public ProtobufLogProperty(IMessage message)
        {
            _message = message;
        }

        public override void Render(TextWriter output, string? format = null, IFormatProvider? formatProvider = null)
        {
            var writer = output as StreamWriter;
            _message.WriteDelimitedTo(writer?.BaseStream);
        }
    }
}
