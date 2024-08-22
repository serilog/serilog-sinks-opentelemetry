using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.FileFallback;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class FilesystemFallbackTests
{
    [Fact]
    public async Task WhenLoggingEnabledToFallback_WithProtobuf_LogsSentToFallback()
    {
        await BaseLogTest(LogFormat.Protobuf, path =>
        {
            using var logStream = System.IO.File.OpenRead(path);
            var logs = new List<ExportLogsServiceRequest>();
            while (logStream.Position < logStream.Length)
            {
                logs.Add(ExportLogsServiceRequest.Parser.ParseDelimitedFrom(logStream));
            }
            return logs;
        });
    }

    [Fact]
    public async Task WhenLoggingEnabledToFallback_WithNDJson_LogsSentToFallback()
    {
        await BaseLogTest(LogFormat.NDJson, JsonReader);
    }

    private List<ExportLogsServiceRequest> JsonReader(string path)
    {
        if(!System.IO.File.Exists(path))
        {
            return new ();
        }

        using var reader = new StreamReader(path);
        var logs = new List<ExportLogsServiceRequest>();
        string? line = null;
        while ((line = reader.ReadLine()) != null)
        {
            logs.Add(ExportLogsServiceRequest.Parser.ParseJson(line));
        }
        return logs;
    }

    [Fact]
    public async Task WhenLoggingEnabledToFallback_Exception_ExceptionsStillThrown()
    {
        // Arrange
        using var tmp = TempFolder.ForCaller();
        var nonExistent = tmp.AllocateFile("log");
        var contextType = typeof(OtlpEventBuilderTests);
        var events = CollectingSink.Collect(log => log.ForContext(contextType).Information("Hello, world!"));
        var exporter = new FailingExporter(new InvalidOperationException("test"));

        // Act
        var export = async () => await ExportFallbackAsync(events, LogFormat.NDJson, nonExistent.FileConfigurationPath, exporter);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(export);
    }

    [Fact]
    public async Task WhenLoggingEnabledToFallback_NoFailure_NoLoggingToFallback()
    {
        // Arrange
        using var tmp = TempFolder.ForCaller();
        var nonExistent = tmp.AllocateFile("log");
        var contextType = typeof(OtlpEventBuilderTests);
        var events = CollectingSink.Collect(log => log.ForContext(contextType).Information("Hello, world!"));
        var exporter = new CollectingExporter();

        // Act
        var export = async () => await ExportFallbackAsync(events, LogFormat.NDJson, nonExistent.FileConfigurationPath, exporter);
        await export();
        await export(); // multiple logs
        var logs = JsonReader(nonExistent.RollingFilePath);

        // Assert
        Assert.Empty(logs);
        Assert.True(exporter.ExportLogsServiceRequests.Count == 2);
    }

    static async Task BaseLogTest(LogFormat logFormat, Func<string, List<ExportLogsServiceRequest>> fileReader)
    {
        // Arrange
        using var tmp = TempFolder.ForCaller();
        var nonExistent = tmp.AllocateFile("log");
        var contextType = typeof(OtlpEventBuilderTests);
        var events = CollectingSink.Collect(log => log.ForContext(contextType).Information("Hello, world!"));
        var exporter = new FailingExporter();

        // Act
        var export = async () => await ExportFallbackAsync(events, logFormat, nonExistent.FileConfigurationPath, exporter);
        await export();
        await export(); // multiple logs
        var logs = fileReader(nonExistent.RollingFilePath);

        // Assert
        Assert.True(logs.Count == 2);
        var actualResources = ResourceAttributeSelector(logs.SelectMany(log => log.ResourceLogs));
        var expectedResources = ResourceAttributeSelector(exporter.ExportLogsServiceRequests.SelectMany(log => log.ResourceLogs));

        Assert.Equal(expectedResources,
            actualResources);
    }

    static IEnumerable<string> ResourceAttributeSelector(IEnumerable<ResourceLogs> resourceLogs)
            => resourceLogs.SelectMany(resource => resource.Resource.Attributes.Select(attr => attr.Key.ToString()));

    static async Task ExportFallbackAsync(IReadOnlyCollection<LogEvent> events, LogFormat logFormat, string path, IExporter exporter)
    {
        using var fileFallback = new ConcreteFileFallback(FileSystemFallback.Configure(
                    fs =>
                    {
                        fs.Path = path;
                        fs.RollingInterval = RollingInterval.Day;
                        fs.Shared = true;
                    },
                    logFormat));
        var sink = new OpenTelemetryLogsSink(exporter,
            null,
            new Dictionary<string, object>(),
            OpenTelemetrySinkOptions.DefaultIncludedData,
            fileFallback);
        await sink.EmitBatchAsync(events);
    }
}
