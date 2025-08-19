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
            ["Payload"] = new StructureValue(
            [
                // Particulars of the JSON structure are unimportant, this is handed of to Serilog's default
                // JSON value formatter.
                new LogEventProperty("a", new ScalarValue(42))
            ])
        };

        var actual = CleanMessageTemplateFormatter.Format(template, properties, null);

        // The default formatter would produce "Received {a = 42}" here.
        Assert.Equal("Received {\"a\":42}", actual);
    }

    [Fact]
    public void CustomFormatterWorksForScalarValues()
    {
        var template = new MessageTemplateParser().Parse("Event occurred at: {Timestamp}");
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45);
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Timestamp"] = new ScalarValue(timestamp)
        };

        var formatProvider = new CustomDateTimeFormatProvider();
        var actual = CleanMessageTemplateFormatter.Format(template, properties, formatProvider);

        // The CustomDateTimeFormatProvider should be invoked to format the DateTime object
        Assert.Equal("Event occurred at: CUSTOM(2024-01-15 14:30:45)", actual);
    }

    [Fact]
    public void CustomFormatterWorksWithMultipleScalarValues()
    {
        var template = new MessageTemplateParser().Parse("Events: {Start} and {End}");
        var start = new DateTime(2024, 1, 15, 9, 0, 0);
        var end = new DateTime(2024, 1, 15, 17, 0, 0);
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Start"] = new ScalarValue(start),
            ["End"] = new ScalarValue(end)
        };

        var formatProvider = new CustomDateTimeFormatProvider();
        var actual = CleanMessageTemplateFormatter.Format(template, properties, formatProvider);

        Assert.Equal($"Events: CUSTOM(2024-01-15 09:00:00) and CUSTOM(2024-01-15 17:00:00)", actual);
    }

    [Fact]
    public void CustomFormatterWorksWithMixedTypes()
    {
        var template = new MessageTemplateParser().Parse("Event at {Timestamp} with {Count} items");
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45);
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Timestamp"] = new ScalarValue(timestamp),
            ["Count"] = new ScalarValue(42)
        };

        var formatProvider = new CustomDateTimeFormatProvider();
        var actual = CleanMessageTemplateFormatter.Format(template, properties, formatProvider);

        // NOTE: DateTime should be custom formatted, Count should use default formatting
        Assert.Equal("Event at CUSTOM(2024-01-15 14:30:45) with 42 items", actual);
    }

    [Fact]
    public void CustomFormatterWorksWithAlignment()
    {
        var template = new MessageTemplateParser().Parse("Event: {Timestamp,30}");
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45);
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Timestamp"] = new ScalarValue(timestamp)
        };

        var formatProvider = new CustomDateTimeFormatProvider();
        var actual = CleanMessageTemplateFormatter.Format(template, properties, formatProvider);

        var expected = $"Event:    CUSTOM(2024-01-15 14:30:45)";
        Assert.Equal(expected, actual);
    }
}

class CustomDateTimeFormatProvider : IFormatProvider, ICustomFormatter
{
    public const string DateTimeFormatting = "CUSTOM-DATE-TIME-FORMATTING";
    public object? GetFormat(Type? formatType) =>
        formatType == typeof(ICustomFormatter)
            ? this
            : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg switch
    {
        DateTime dateTime => $"CUSTOM({dateTime:yyyy-MM-dd HH:mm:ss})",
        IFormattable formattable => formattable.ToString(format, formatProvider),
        _ => arg?.ToString() ?? ""
    };
}
