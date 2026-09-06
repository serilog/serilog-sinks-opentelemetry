using System.Diagnostics;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Core;
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
        var link1 = new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None));
        var link2 = new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None));
        using var activity = Some.Activity();
        var events = CollectingSink.Collect(log => ForSpan(log, start, kind, parent, [link1, link2]).Information("Hello, {Name}!", "World"));
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
        Assert.Equal(2, span.Links.Count);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetryTraceId(link1.Context.TraceId.ToHexString()), span.Links[0].TraceId);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetrySpanId(link1.Context.SpanId.ToHexString()), span.Links[0].SpanId);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetryTraceId(link2.Context.TraceId.ToHexString()), span.Links[1].TraceId);
        Assert.Equal(PrimitiveConversions.ToOpenTelemetrySpanId(link2.Context.SpanId.ToHexString()), span.Links[1].SpanId);
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
    static ILogger ForSpan(ILogger logger, DateTime? start = null, ActivityKind kind = ActivityKind.Internal, ActivitySpanId? parentId = null, IEnumerable<ActivityLink>? links = null)
    {
        var result = logger.ForContext("SpanStartTimestamp", start ?? Some.UtcDateTime())
            .ForContext("SpanKind", kind);

        if (parentId != null)
            result = result.ForContext("ParentSpanId", parentId.Value);

        if (links is not null)
            result = result.ForContext(new SpanLinksEnricher(links));

        return result;
    }

    // Wraps each link in a value that renders as a W3C traceparent header
    sealed class SpanLinksEnricher(IEnumerable<ActivityLink> links) : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(
                "SpanLinks",
                new SequenceValue(links.Select(l => new ScalarValue(new Traceparent(l))))));
        }
    }
}
