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

using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Trace;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class ConvertTest
{
    [Fact]
    public void TestProcessMessage()
    {
        var logRecord = new LogRecord();
        Convert.ProcessMessage(logRecord, null);
        Assert.Null(logRecord.Body);

        Convert.ProcessMessage(logRecord, "");
        Assert.Null(logRecord.Body);

        Convert.ProcessMessage(logRecord, "\t\f ");
        Assert.Null(logRecord.Body);

        var message = "log message";
        Convert.ProcessMessage(logRecord, message);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(logRecord.Body.StringValue, message);
    }

    [Fact]
    public void TestProcessMessageTemplate()
    {
        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent();

        Convert.ProcessMessageTemplate(logRecord, logEvent);

        var templateString = logEvent.MessageTemplate.ToString();
        var templateStringHash = ConvertUtils.Md5Hash(templateString);
        var templateKeyValue = ConvertUtils.NewStringAttribute(Convert.MESSAGE_TEMPLATE, templateString);
        var templateHashKeyValue = ConvertUtils.NewStringAttribute(Convert.MESSAGE_TEMPLATE_HASH, templateStringHash);

        Assert.Equal(2, logRecord.Attributes.Count);
        Assert.NotEqual(-1, logRecord.Attributes.IndexOf(templateKeyValue));
        Assert.NotEqual(-1, logRecord.Attributes.IndexOf(templateHashKeyValue));
    }

    [Fact]
    public void TestProcessLevel()
    {
        var template = new MessageTemplate(new List<MessageTemplateToken>());
        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent();

        Convert.ProcessLevel(logRecord, logEvent);

        Assert.Equal(LogEventLevel.Warning.ToString(), logRecord.SeverityText);
        Assert.Equal(SeverityNumber.Warn, logRecord.SeverityNumber);
    }

    [Fact]
    public void TestProcessProperties()
    {
        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent();

        var traceGuid = "01020304050607080910111213141516";
        var traceBytes = ConvertUtils.ToOpenTelemetryTraceId(traceGuid);
        var spanGuid = "0102030405060708";
        var spanBytes = ConvertUtils.ToOpenTelemetrySpanId(spanGuid);

        var traceId = new LogEventProperty(TraceIdEnricher.TRACE_ID_PROPERTY_NAME, new ScalarValue(traceGuid));
        var spanId = new LogEventProperty(TraceIdEnricher.SPAN_ID_PROPERTY_NAME, new ScalarValue(spanGuid));
        var prop = new LogEventProperty("property_name", new ScalarValue("ok"));
        var propertyKeyValue = ConvertUtils.NewStringAttribute("property_name", "ok");

        logEvent.AddOrUpdateProperty(traceId);
        logEvent.AddOrUpdateProperty(spanId);
        logEvent.AddOrUpdateProperty(prop);

        Convert.ProcessProperties(logRecord, logEvent);

        Assert.Single(logRecord.Attributes);
        Assert.Equal(traceBytes, logRecord.TraceId);
        Assert.Equal(spanBytes, logRecord.SpanId);
        Assert.NotEqual(-1, logRecord.Attributes.IndexOf(propertyKeyValue));
    }

    [Fact]
    public void TestTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var nowNano = ConvertUtils.ToUnixNano(now);

        var logRecord = new LogRecord();
        var logEvent = TestUtils.CreateLogEvent(timestamp: now);

        Convert.ProcessTimestamp(logRecord, logEvent);

        Assert.Equal(nowNano, logRecord.TimeUnixNano);
    }

}