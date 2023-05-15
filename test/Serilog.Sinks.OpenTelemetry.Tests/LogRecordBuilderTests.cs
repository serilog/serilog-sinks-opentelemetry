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
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class LogRecordBuilderTests
{
    [Fact]
    public void TestProcessMessage()
    {
        var logRecord = new LogRecord();

        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: ""), null);
        Assert.Null(logRecord.Body);

        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: "\t\f "), null);
        Assert.Null(logRecord.Body);

        const string message = "log message";
        LogRecordBuilder.ProcessMessage(logRecord, Some.SerilogEvent(messageTemplate: message), null);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(message, logRecord.Body.StringValue);
    }

    [Fact]
    public void TestProcessLevel()
    {
        var logRecord = new LogRecord();
        var logEvent = Some.SerilogEvent();

        LogRecordBuilder.ProcessLevel(logRecord, logEvent);

        Assert.Equal(LogEventLevel.Warning.ToString(), logRecord.SeverityText);
        Assert.Equal(SeverityNumber.Warn, logRecord.SeverityNumber);
    }

    [Fact]
    public void TestProcessProperties()
    {
        var logRecord = new LogRecord();
        var logEvent = Some.SerilogEvent();
        
        var prop = new LogEventProperty("property_name", new ScalarValue("ok"));
        var propertyKeyValue = PrimitiveConversions.NewStringAttribute("property_name", "ok");

        logEvent.AddOrUpdateProperty(prop);

        LogRecordBuilder.ProcessProperties(logRecord, logEvent);

        Assert.Contains(propertyKeyValue, logRecord.Attributes);
    }

    [Fact]
    public void TestTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var nowNano = PrimitiveConversions.ToUnixNano(now);

        var logRecord = new LogRecord();
        var logEvent = Some.SerilogEvent(timestamp: now);

        LogRecordBuilder.ProcessTimestamp(logRecord, logEvent);

        Assert.Equal(nowNano, logRecord.TimeUnixNano);
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
            var logEvent = Some.SerilogEvent(ex: ex);

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
        
        var logRecord = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateMD5HashAttribute, new());
        
        var expectedHash = PrimitiveConversions.Md5Hash(Some.TestMessageTemplate);
        var expectedAttribute = new KeyValue { Key = SemanticConventions.AttributeMessageTemplateMD5Hash, Value = new() { StringValue = expectedHash }};
        Assert.Contains(expectedAttribute, logRecord.Attributes);
    }
    
    [Fact]
    public void IncludeMessageTemplateText()
    {
        var logEvent = Some.SerilogEvent(messageTemplate: Some.TestMessageTemplate);
        
        var logRecord = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.MessageTemplateTextAttribute, new());

        var expectedAttribute = new KeyValue { Key = SemanticConventions.AttributeMessageTemplateText, Value = new() { StringValue = Some.TestMessageTemplate }};
        Assert.Contains(expectedAttribute, logRecord.Attributes);
    }
    
    [Fact]
    public void IncludeTraceIdWhenActivityIsNull()
    {
        Assert.Null(Activity.Current);

        var collector = new ActivityContextCollector();
        
        var logEvent = Some.SerilogEvent();
        collector.CollectFor(logEvent);
        
        var logRecord = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.TraceIdField | IncludedData.SpanIdField, collector);

        Assert.True(logRecord.TraceId.IsEmpty);
        Assert.True(logRecord.SpanId.IsEmpty);
    }

    [Fact]
    public void IncludeTraceIdAndSpanId()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("test.activity", "1.0.0");
        using var activity = source.StartActivity();
        Assert.NotNull(Activity.Current);

        var collector = new ActivityContextCollector();
        
        var logEvent = Some.SerilogEvent();
        collector.CollectFor(logEvent);
        
        var logRecord = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.TraceIdField | IncludedData.SpanIdField, collector);

        Assert.Equal(logRecord.TraceId, PrimitiveConversions.ToOpenTelemetryTraceId(Activity.Current.TraceId.ToHexString()));
        Assert.Equal(logRecord.SpanId, PrimitiveConversions.ToOpenTelemetrySpanId(Activity.Current.SpanId.ToHexString()));
    }
}
