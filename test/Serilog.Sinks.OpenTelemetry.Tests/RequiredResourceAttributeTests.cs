using System.Globalization;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;
#if NETFRAMEWORK
#endif

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class RequiredResourceAttributeTests
{
    [Fact]
    public void ServiceNameIsPreservedWhenPresent()
    {
        var supplied = Some.String();
        var ra = new Dictionary<string, object>
        {
            ["service.name"] = supplied
        };

        var actual = RequiredResourceAttributes.AddDefaults(ra);
        
        Assert.Equal(supplied, actual["service.name"]);
    }

    [Fact]
    public void MissingServiceNameDefaultsToExecutableName()
    {
        var actual = RequiredResourceAttributes.AddDefaults(new Dictionary<string, object>());
        
        Assert.StartsWith("unknown_service:", (string)actual["service.name"]);
    }

    [Fact]
    public void MissingTelemetrySdkGroupDefaultsToKnownValues()
    {
        var actual = RequiredResourceAttributes.AddDefaults(new Dictionary<string, object>());
        Assert.Equal("serilog", actual["telemetry.sdk.name"]);
        Assert.Equal("dotnet", actual["telemetry.sdk.language"]);
        // First character of the version is always expected to be numeric.
        Assert.True(int.TryParse(((string)actual["telemetry.sdk.version"])[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
    }
}
