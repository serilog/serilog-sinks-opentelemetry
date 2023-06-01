using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class PublicApiVisibilityTests
{
    [Fact]
    public void PublicApiShouldNotChangeUnintentionally()
    {
        var assembly = typeof(OpenTelemetryLoggerConfigurationExtensions).Assembly;
        var publicApi = assembly.GeneratePublicApi(
            new()
            {
                IncludeAssemblyAttributes = false,
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" },
            });

        publicApi.ShouldMatchApproved(options =>
        {
            options.WithFilenameGenerator((_, _, fileType, fileExtension) => $"{nameof(PublicApiVisibilityTests)}.{fileType}.{fileExtension}");
        });
    }    
}
