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
using Serilog.Sinks.OpenTelemetry.Formatting;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;

namespace Serilog.Sinks.OpenTelemetry;

static class LogRecordBuilder
{
    /// <summary>
    /// A https://messagetemplates.org template, as text. For example, the string <c>Hello {Name}!</c>.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/semantic_conventions/ and
    /// <see cref="TraceSemanticConventions"/>.</remarks>
    public  const string AttributeMessageTemplateText = "message_template.text";

    /// <summary>
    /// A https://messagetemplates.org template, hashed using MD5 and encoded as a 128-bit hexadecimal value.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/semantic_conventions/ and
    /// <see cref="TraceSemanticConventions"/>.</remarks>
    public const string AttributeMessageTemplateMD5Hash = "message_template.hash.md5";

    public static LogRecord ToLogRecord(LogEvent logEvent, IFormatProvider? formatProvider, IncludedData includedFields, ActivityContextCollector activityContextCollector)
    {
        var logRecord = new LogRecord();

        ProcessProperties(logRecord, logEvent);
        ProcessTimestamp(logRecord, logEvent);
        ProcessMessage(logRecord, logEvent, formatProvider);
        ProcessLevel(logRecord, logEvent);
        ProcessException(logRecord, logEvent);
        ProcessIncludedFields(logRecord, logEvent, includedFields, activityContextCollector);

        return logRecord;
    }

    public static void ProcessMessage(LogRecord logRecord, LogEvent logEvent, IFormatProvider? formatProvider)
    {
        var renderedMessage = CleanMessageTemplateFormatter.Format(logEvent.MessageTemplate, logEvent.Properties, formatProvider);
        if (renderedMessage.Trim() != "")
        {
            logRecord.Body = new AnyValue
            {
                StringValue = renderedMessage
            };
        }
    }

    public static void ProcessLevel(LogRecord logRecord, LogEvent logEvent)
    {
        var level = logEvent.Level;
        logRecord.SeverityText = level.ToString();
        logRecord.SeverityNumber = PrimitiveConversions.ToSeverityNumber(level);
    }

    public static void ProcessProperties(LogRecord logRecord, LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties)
        {
            var v = PrimitiveConversions.ToOpenTelemetryAnyValue(property.Value);
            if (v != null)
            {
                logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(property.Key, v));
            }
        }
    }

    public static void ProcessTimestamp(LogRecord logRecord, LogEvent logEvent)
    {
        logRecord.TimeUnixNano = PrimitiveConversions.ToUnixNano(logEvent.Timestamp);
    }

    public static void ProcessException(LogRecord logRecord, LogEvent logEvent)
    {
        var ex = logEvent.Exception;
        if (ex != null)
        {
            var attrs = logRecord.Attributes;

            attrs.Add(PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionType, ex.GetType().ToString()));

            if (ex.Message != "")
            {
                attrs.Add(PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionMessage, ex.Message));
            }

            if (ex.ToString() != "")
            {
                attrs.Add(PrimitiveConversions.NewStringAttribute(TraceSemanticConventions.AttributeExceptionStacktrace, ex.ToString()));
            }
        }
    }

    static void ProcessIncludedFields(LogRecord logRecord, LogEvent logEvent, IncludedData includedFields, ActivityContextCollector activityContextCollector)
    {
        if ((includedFields & (IncludedData.TraceIdField | IncludedData.SpanIdField)) != IncludedData.None)
        {
            var activityContext = activityContextCollector.GetFor(logEvent);
            
            if (activityContext is var (activityTraceId, activitySpanId))
            {
                if ((includedFields & IncludedData.TraceIdField) != IncludedData.None)
                {
                    logRecord.TraceId = PrimitiveConversions.ToOpenTelemetryTraceId(activityTraceId.ToHexString());
                }

                if ((includedFields & IncludedData.SpanIdField) != IncludedData.None)
                {
                    logRecord.SpanId = PrimitiveConversions.ToOpenTelemetrySpanId(activitySpanId.ToHexString());
                }
            }
        }

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(AttributeMessageTemplateText, new()
            {
                StringValue = logEvent.MessageTemplate.Text
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(PrimitiveConversions.NewAttribute(AttributeMessageTemplateMD5Hash, new()
            {
                StringValue = PrimitiveConversions.Md5Hash(logEvent.MessageTemplate.Text)
            }));
        }
    }
}
