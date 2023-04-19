using System.Security.Cryptography.X509Certificates;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.OpenTelemetry.Formatting;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class CleanMessageTemplateFormatterTests
{
    [Fact]
    public void FormatsEmbeddedStringsWithoutQuoting()
    {
        var template = new MessageTemplateParser().Parse("Hello, {Name}!");
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Name"] = new ScalarValue("world")
        };

        var actual = CleanMessageTemplateFormatter.Format(template, properties, null);
        
        // The default formatter would produce "Hello, \"world\"!" here.
        Assert.Equal("Hello, world!", actual);
    }

    [Fact]
    public void FormatsEmbeddedStructuresAsJson()
    {
        var template = new MessageTemplateParser().Parse("Received {Payload}");
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Payload"] = new StructureValue(new []
            {
                // Particulars of the JSON structure are unimportant, this is handed of to Serilog's default
                // JSON value formatter.
                new LogEventProperty("a", new ScalarValue(42))
            })
        };

        var actual = CleanMessageTemplateFormatter.Format(template, properties, null);
        
        // The default formatter would produce "Received {a = 42}" here.
        Assert.Equal("Received {\"a\":42}", actual);
    }
}
