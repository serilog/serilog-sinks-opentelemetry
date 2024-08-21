using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.OpenTelemetry.FileFallback.Formatters
{
    internal class OtlpFormatter : ITextFormatter
    {
        public static readonly string OtlpMessageContents = "OtlpMessageContents";

        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (!logEvent.Properties.ContainsKey(OtlpMessageContents))
            {
                return;
            }

            var exportServiceRequest = logEvent.Properties["OtlpMessageContents"];
            exportServiceRequest.Render(output);
        }
    }
}
