using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetrySinkTests
{
    [Fact]
    public async Task DefaultScopeIsNull()
    {
        var events = CollectingSink.Collect(log => log.Information("Hello, world!"));
        var request = await ExportAsync(events);
        var resourceLogs = Assert.Single(request.ResourceLogs);
        var scopeLogs = Assert.Single(resourceLogs.ScopeLogs);
        Assert.Null(scopeLogs.Scope);
    }
    
    [Fact]
    public async Task SourceContextNameIsInstrumentationScope()
    {
        var contextType = typeof(LogRecordBuilderTests);
        var events = CollectingSink.Collect(log => log.ForContext(contextType).Information("Hello, world!"));
        var request = await ExportAsync(events);
        var resourceLogs = Assert.Single(request.ResourceLogs);
        var scopeLogs = Assert.Single(resourceLogs.ScopeLogs);
        Assert.Equal(contextType.FullName, scopeLogs.Scope.Name);
    }
    
    [Fact]
    public async Task ScopeLogsAreGrouped()
    {
        var events = CollectingSink.Collect(log =>
        {
            log.ForContext(Constants.SourceContextPropertyName, "A").Information("Hello, world!");
            log.ForContext(Constants.SourceContextPropertyName, "B").Information("Hello, world!");
            log.ForContext(Constants.SourceContextPropertyName, "A").Information("Hello, world!");
            log.Information("Hello, world!");
        });
        var request = await ExportAsync(events);
        var resourceLogs = Assert.Single(request.ResourceLogs);
        Assert.Equal(3, resourceLogs.ScopeLogs.Count);
        Assert.Equal(4, resourceLogs.ScopeLogs.SelectMany(s => s.LogRecords).Count());
        Assert.Equal(2, resourceLogs.ScopeLogs.Single(r => r.Scope?.Name == "A").LogRecords.Count);
        Assert.Single(resourceLogs.ScopeLogs.Single(r => r.Scope?.Name == "B").LogRecords);
        Assert.Single(resourceLogs.ScopeLogs.Single(r => r.Scope == null).LogRecords);
    }

    static async Task<ExportLogsServiceRequest> ExportAsync(IReadOnlyCollection<LogEvent> events)
    {
        var exporter = new CollectingExporter();
        var sink = new OpenTelemetrySink(exporter, null, new Dictionary<string, object>(), OpenTelemetrySinkOptions.DefaultIncludedData);
        await sink.EmitBatchAsync(events);
        return Assert.Single(exporter.Requests);
    }
}