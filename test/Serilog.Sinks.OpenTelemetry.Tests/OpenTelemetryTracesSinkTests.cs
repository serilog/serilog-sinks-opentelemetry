using System.Diagnostics;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class OpenTelemetryTracesSinkTests
{
    [Fact]
    public async Task SpanCarriesExpectedSimpleProperties()
    {
        var start = Some.UtcDateTime();
        var parent = ActivitySpanId.CreateRandom();
        var kind = ActivityKind.Consumer;
        using var activity = Some.Activity();
        var events = CollectingSink.Collect(log => ForSpan(log, start, kind, parent).Information("Hello, {Name}!", "World"));
        var request = await ExportAsync(events);
        var resourceSpans = Assert.Single(request.ResourceSpans);
        var scopeSpans = Assert.Single(resourceSpans.ScopeSpans);
        var span = Assert.Single(scopeSpans.Spans);
        Assert.Equal("Hello, {Name}!", span.Name);
        Assert.Equal(PrimitiveConversions.ToUnixNano(start), span.StartTimeUnixNano);
        Assert.Equal(PrimitiveConversions.ToUnixNano(events.Single().Timestamp), span.EndTimeUnixNano);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetrySpanId(parent.ToString()), span.ParentSpanId);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetrySpanKind(kind), span.Kind);
        Assert.Contains(span.Attributes, kv => kv.Key == "Name" && kv.Value.StringValue == "World");
    }

    [Fact]
    public async Task DefaultScopeIsNull()
    {
        using var activity = Some.Activity();
        var events = CollectingSink.Collect(log => ForSpan(log).Information("Hello, world!"));
        var request = await ExportAsync(events);
        var resourceSpans = Assert.Single(request.ResourceSpans);
        var scopeSpans = Assert.Single(resourceSpans.ScopeSpans);
        Assert.Null(scopeSpans.Scope);
    }

    [Fact]
    public async Task SourceContextNameIsInstrumentationScope()
    {
        using var activity = Some.Activity();
        var contextType = typeof(OtlpEventBuilderTests);
        var events = CollectingSink.Collect(log => ForSpan(log).ForContext(contextType).Information("Hello, world!"));
        var request = await ExportAsync(events);
        var resourceSpans = Assert.Single(request.ResourceSpans);
        var scopeSpans = Assert.Single(resourceSpans.ScopeSpans);
        Assert.Equal(contextType.FullName, scopeSpans.Scope.Name);
    }
    
    [Fact]
    public async Task ScopeSpansAreGrouped()
    {
        using var activity = Some.Activity();
        var events = CollectingSink.Collect(log =>
        {
            ForSpan(log).ForContext(Core.Constants.SourceContextPropertyName, "A").Information("Hello, world!");
            ForSpan(log).ForContext(Core.Constants.SourceContextPropertyName, "B").Information("Hello, world!");
            ForSpan(log).ForContext(Core.Constants.SourceContextPropertyName, "A").Information("Hello, world!");
            ForSpan(log).Information("Hello, world!");
        });
        var request = await ExportAsync(events);
        var resourceSpans = Assert.Single(request.ResourceSpans);
        Assert.Equal(3, resourceSpans.ScopeSpans.Count);
        Assert.Equal(4, resourceSpans.ScopeSpans.SelectMany(s => s.Spans).Count());
        Assert.Equal(2, resourceSpans.ScopeSpans.Single(r => r.Scope?.Name == "A").Spans.Count);
        Assert.Single(resourceSpans.ScopeSpans.Single(r => r.Scope?.Name == "B").Spans);
        Assert.Single(resourceSpans.ScopeSpans.Single(r => r.Scope == null).Spans);
    }

    static async Task<ExportTraceServiceRequest> ExportAsync(IReadOnlyCollection<LogEvent> events)
    {
        var exporter = new CollectingExporter();
        var sink = new OpenTelemetryTracesSink(exporter, new Dictionary<string, object>(), OpenTelemetrySinkOptions.DefaultIncludedData);
        await sink.EmitBatchAsync(events);
        return Assert.Single(exporter.ExportTraceServiceRequests);
    }

    // Produces a completely imaginary span, which will be inconsistent with Activity.Current except to carry the
    // same span id.
    static ILogger ForSpan(ILogger logger, DateTime? start = null, ActivityKind kind = ActivityKind.Internal, ActivitySpanId? parentId = null)
    {
        var result = logger.ForContext("SpanStartTimestamp", start ?? Some.UtcDateTime())
            .ForContext("SpanKind", kind);

        if (parentId != null)
            result = result.ForContext("ParentSpanId", parentId.Value);

        return result;
    }
}
