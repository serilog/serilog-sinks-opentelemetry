using Serilog.Sinks.OpenTelemetry.Configuration;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class BaggageFormatTests
{
    public static TheoryData<string, (string, string)[]> Cases => new()
    {
        { "", [] },
        { " ", [] },
        { "a=", [("a", "")] },
        { "abc=def", [("abc", "def")] },
        { "abc= def ", [("abc", "def")] },
        { "abc=def,ghi=jkl", [("abc", "def"), ("ghi", "jkl")] },
        { "a=1%202", [("a", "1 2")] },
    };
    
    [Theory, MemberData(nameof(Cases))]
    public void BaggageStringsAreDecoded(string baggageString, IEnumerable<(string, string)> expected)
    {
        var actual = BaggageFormat.DecodeBaggageString(baggageString, Some.String());
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(",")]
    [InlineData(", ")]
    [InlineData("a")]
    [InlineData("=")]
    [InlineData(",=")]
    public void InvalidBaggageStringsAreRejected(string baggageString)
    {
        Assert.Throws<ArgumentException>(() => BaggageFormat.DecodeBaggageString(baggageString, Some.String()).ToList());
    }
}