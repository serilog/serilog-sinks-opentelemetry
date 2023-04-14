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
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Trace;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class LogRecordFactoryTests
{
    [Fact]
    public void TestProcessMessage()
    {
        var logRecord = new LogRecord();

        LogRecordFactory.ProcessMessage(logRecord, TestUtils.CreateLogEvent(messageTemplate: ""), null);
        Assert.Null(logRecord.Body);

        LogRecordFactory.ProcessMessage(logRecord, TestUtils.CreateLogEvent(messageTemplate: "\t\f "), null);
        Assert.Null(logRecord.Body);

        const string message = "log message";
        LogRecordFactory.ProcessMessage(logRecord, TestUtils.CreateLogEvent(messageTemplate: message), null);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(message, logRecord.Body.StringValue);
    }

    [Fact]
    public void TestProcessLevel()
    {
        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent();

        LogRecordFactory.ProcessLevel(logRecord, logEvent);

        Assert.Equal(LogEventLevel.Warning.ToString(), logRecord.SeverityText);
        Assert.Equal(SeverityNumber.Warn, logRecord.SeverityNumber);
    }

    [Fact]
    public void TestProcessProperties()
    {
        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent();
        
        var prop = new LogEventProperty("property_name", new ScalarValue("ok"));
        var propertyKeyValue = ConvertUtils.NewStringAttribute("property_name", "ok");

        logEvent.AddOrUpdateProperty(prop);

        LogRecordFactory.ProcessProperties(logRecord, logEvent);

        Assert.Single(logRecord.Attributes);
        Assert.NotEqual(-1, logRecord.Attributes.IndexOf(propertyKeyValue));
    }

    [Fact]
    public void TestTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var nowNano = ConvertUtils.ToUnixNano(now);

        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent(timestamp: now);

        LogRecordFactory.ProcessTimestamp(logRecord, logEvent);

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
            var logEvent = TestUtils.CreateLogEvent(ex: ex);

            LogRecordFactory.ProcessException(logRecord, logEvent);

            var typeKeyValue = ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionType, error.GetType().ToString());
            var messageKeyValue = ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionMessage, error.Message);

            Assert.Equal(3, logRecord.Attributes.Count);
            Assert.NotEqual(-1, logRecord.Attributes.IndexOf(typeKeyValue));
            Assert.NotEqual(-1, logRecord.Attributes.IndexOf(messageKeyValue));

            Assert.NotNull(ex.StackTrace);
            if (ex.StackTrace != null)
            {
                var traceKeyValue = ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionStacktrace, ex.ToString());
                Assert.NotEqual(-1, logRecord.Attributes.IndexOf(traceKeyValue));
            }
        }
    }

    [Fact]
    public void IncludeMessageTemplateMD5Hash()
    {
        var logEvent = TestUtils.CreateLogEvent(messageTemplate: TestUtils.TestMessageTemplate);
        
        LogRecordFactory.ToLogRecord(logEvent, null, LogRecordData.MessageTemplateMD5HashAttribute);

        var expectedHash = ConvertUtils.Md5Hash(TestUtils.TestMessageTemplate);
        var expectedProperty = new KeyValuePair<string, LogEventPropertyValue>(LogRecordFactory.AttributeMessageTemplateMD5Hash, new ScalarValue(expectedHash));
        Assert.Contains(expectedProperty, logEvent.Properties);
    }
    
    [Fact]
    public void IncludeMessageTemplateText()
    {
        var logEvent = TestUtils.CreateLogEvent(messageTemplate: TestUtils.TestMessageTemplate);
        
        LogRecordFactory.ToLogRecord(logEvent, null, LogRecordData.MessageTemplateTextAttribute);

        var expectedProperty = new KeyValuePair<string, LogEventPropertyValue>(LogRecordFactory.AttributeMessageTemplateMD5Hash, new ScalarValue(TestUtils.TestMessageTemplate));
        Assert.Contains(expectedProperty, logEvent.Properties);
    }
    
    [Fact]
    public void IncludeTraceIdWhenActivityIsNull()
    {
        Assert.Null(Activity.Current);
        
        var logEvent = TestUtils.CreateLogEvent();
        var logRecord = LogRecordFactory.ToLogRecord(logEvent, null, LogRecordData.TraceIdField | LogRecordData.SpanIdField);

        Assert.Null(logRecord.TraceId);
        Assert.Null(logRecord.SpanId);
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
        
        var logEvent = TestUtils.CreateLogEvent();
        var logRecord = LogRecordFactory.ToLogRecord(logEvent, null, LogRecordData.TraceIdField | LogRecordData.SpanIdField);

        Assert.Equal(logRecord.TraceId, ConvertUtils.ToOpenTelemetryTraceId(Activity.Current.TraceId.ToHexString()));
        Assert.Equal(logRecord.SpanId, ConvertUtils.ToOpenTelemetrySpanId(Activity.Current.SpanId.ToHexString()));
    }
}
