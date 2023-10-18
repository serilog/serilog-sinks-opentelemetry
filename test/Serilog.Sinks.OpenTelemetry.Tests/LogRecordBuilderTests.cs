// Copyright 2022 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Trace;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Serilog.Sinks.OpenTelemetry.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class LogRecordBuilderTests
{
    [Fact]
    public void TestProcessMessage()
    {
        var logRecord = new LogRecord();

        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: ""), OpenTelemetrySinkOptions.DefaultIncludedData, null);
        Assert.Null(logRecord.Body);

        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: "\t\f "), OpenTelemetrySinkOptions.DefaultIncludedData, null);
        Assert.Null(logRecord.Body);

        const string message = "log message";
        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: message), OpenTelemetrySinkOptions.DefaultIncludedData, null);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(message, logRecord.Body.StringValue);
    }

    [Fact]
    public void TestProcessLevel()
    {
        var logRecord = new LogRecord();
        var logEvent = Some.DefaultSerilogEvent();

        LogRecordBuilder.ProcessLevel(logRecord, logEvent);

        Assert.Equal(LogEventLevel.Warning.ToString(), logRecord.SeverityText);
        Assert.Equal(SeverityNumber.Warn, logRecord.SeverityNumber);
    }

    [Fact]
    public void TestProcessProperties()
    {
        var logRecord = new LogRecord();
        var logEvent = Some.DefaultSerilogEvent();
        
        var prop = new LogEventProperty("property_name", new ScalarValue("ok"));
        var propertyKeyValue = PrimitiveConversions.NewStringAttribute("property_name", "ok");

        logEvent.AddOrUpdateProperty(prop);

        LogRecordBuilder.ProcessProperties(logRecord, logEvent, IncludedData.None, out _);

        Assert.Contains(propertyKeyValue, logRecord.Attributes);
    }

    [Fact]
    public void TestTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var nowNano = PrimitiveConversions.ToUnixNano(now);

        var logRecord = new LogRecord();
        var logEvent = Some.SerilogEvent(Some.TestMessageTemplate, timestamp: now);

        LogRecordBuilder.ProcessTimestamp(logRecord, logEvent);

        Assert.Equal(nowNano, logRecord.TimeUnixNano);
        Assert.Equal(nowNano, logRecord.ObservedTimeUnixNano);
    }

    [Fact]
    public void TestException()
    {
        var error = new Exception("error_message");

        try
        {
            throw error;
        }
        catch (Exception ex)
        {
            var logRecord = new LogRecord();
            var logEvent = Some.SerilogEvent(Some.TestMessageTemplate, ex: ex);

            LogRecordBuilder.ProcessException(logRecord, logEvent);

            var typeKeyValue = PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionType, error.GetType().ToString());
            var messageKeyValue = PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionMessage, error.Message);

            Assert.Equal(3, logRecord.Attributes.Count);
            Assert.NotEqual(-1, logRecord.Attributes.IndexOf(typeKeyValue));
            Assert.NotEqual(-1, logRecord.Attributes.IndexOf(messageKeyValue));

            Assert.NotNull(ex.StackTrace);
            if (ex.StackTrace != null)
            {
                var traceKeyValue = PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionStacktrace, ex.ToString());
                Assert.NotEqual(-1, logRecord.Attributes.IndexOf(traceKeyValue));
            }
        }
    }

    [Fact]
    public void IncludeMessageTemplateMD5Hash()
    {
        var logEvent = Some.SerilogEvent(messageTemplate: Some.TestMessageTemplate);
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateMD5HashAttribute);
        
        var expectedHash = PrimitiveConversions.Md5Hash(Some.TestMessageTemplate);
        var expectedAttribute = new KeyValue { Key = SemanticConventions.AttributeMessageTemplateMD5Hash, Value = new() { StringValue = expectedHash }};
        Assert.Contains(expectedAttribute, logRecord.Attributes);
    }
    
    [Fact]
    public void IncludeMessageTemplateText()
    {
        var messageTemplate = "Hello, {Name}";
        var properties = new List<LogEventProperty> { new("Name", new ScalarValue("World")) };

        var logEvent = Some.SerilogEvent(messageTemplate, properties);
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateTextAttribute);

        var expectedAttribute = new KeyValue { Key = SemanticConventions.AttributeMessageTemplateText, Value = new() { StringValue = messageTemplate } };
        Assert.Contains(expectedAttribute, logRecord.Attributes);
    }
    
    [Fact]
    public void IncludeTraceIdWhenActivityIsNull()
    {
        Assert.Null(Activity.Current);

        var logEvent = Some.DefaultSerilogEvent();
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.TraceIdField | IncludedData.SpanIdField);

        Assert.True(logRecord.TraceId.IsEmpty);
        Assert.True(logRecord.SpanId.IsEmpty);
    }

    [Fact]
    public void IncludeTraceIdAndSpanId()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;

        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("test.activity", "1.0.0");
        using var activity = source.StartActivity();
        Assert.NotNull(Activity.Current);

        var logEvent = CollectingSink.CollectSingle(log => log.Information("Hello, trace and span!"));
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.TraceIdField | IncludedData.SpanIdField);

        Assert.Equal(logRecord.TraceId, PrimitiveConversions.ToOpenTelemetryTraceId(Activity.Current.TraceId.ToHexString()));
        Assert.Equal(logRecord.SpanId, PrimitiveConversions.ToOpenTelemetrySpanId(Activity.Current.SpanId.ToHexString()));
    }

    [Fact]
    public void TemplateBodyIncludesMessageTemplateInBody()
    {
        const string messageTemplate = "Hello, {Name}";
        var properties = new List<LogEventProperty> { new("Name", new ScalarValue("World")) };

        var (logRecord, _) = LogRecordBuilder.ToLogRecord(Some.SerilogEvent(messageTemplate, properties), null, IncludedData.TemplateBody);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(messageTemplate, logRecord.Body.StringValue);
    }
    
    [Fact]
    public void NoRenderingsIncludedWhenNoneInTemplate()
    {
        var logEvent = Some.SerilogEvent(messageTemplate: "Hello, {Name}", properties: new [] { new LogEventProperty("Name", new ScalarValue("World"))});
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateRenderingsAttribute);
        
        Assert.DoesNotContain(SemanticConventions.AttributeMessageTemplateRenderings, logRecord.Attributes.Select(a => a.Key));
    }
    
    [Fact]
    public void RenderingsIncludedWhenPresentInTemplate()
    {
        var logEvent = CollectingSink.CollectSingle(log => log.Information("{First:0} {Second} {Third:0.00}", 123.456, 234.567, 345.678));
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateRenderingsAttribute);
        
        var expectedAttribute = new KeyValue { Key = SemanticConventions.AttributeMessageTemplateRenderings, Value = new()
        {
            ArrayValue = new ArrayValue {
                Values =
                {
                    // Only values for tokens with format strings are included.
                    new AnyValue{ StringValue = "123"},
                    new AnyValue{ StringValue = "345.68"},
                }
            }
        }};
        Assert.Contains(expectedAttribute, logRecord.Attributes);
    }
    
    [Fact]
    public void RenderingsNotIncludedWhenIncludedDataDoesNotSpecifyThem()
    {
        var logEvent = CollectingSink.CollectSingle(log => log.Information("{First:0}", 123.456));
        
        var (logRecord, _) = LogRecordBuilder.ToLogRecord(logEvent, null, OpenTelemetrySinkOptions.DefaultIncludedData);
        
        Assert.DoesNotContain(SemanticConventions.AttributeMessageTemplateRenderings, logRecord.Attributes.Select(a => a.Key));
    }
}
