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

#if FEATURE_ACTIVITY
using System.Diagnostics;
#endif

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Trace;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry;

static class LogRecordFactory
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
    public const string AttributeMessageTemplateMD5Hash = "message_template.md5_hash";

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
        var renderedMessage = logEvent.RenderMessage(formatProvider);
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
        logRecord.SeverityNumber = ConvertUtils.ToSeverityNumber(level);
    }

    public static void ProcessProperties(LogRecord logRecord, LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties)
        {
            var v = ConvertUtils.ToOpenTelemetryAnyValue(property.Value);
            if (v != null)
            {
                logRecord.Attributes.Add(ConvertUtils.NewAttribute(property.Key, v));
            }
        }
    }

    public static void ProcessTimestamp(LogRecord logRecord, LogEvent logEvent)
    {
        logRecord.TimeUnixNano = ConvertUtils.ToUnixNano(logEvent.Timestamp);
    }

    public static void ProcessException(LogRecord logRecord, LogEvent logEvent)
    {
        var ex = logEvent.Exception;
        if (ex != null)
        {
            var attrs = logRecord.Attributes;

            attrs.Add(ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionType, ex.GetType().ToString()));

            if (ex.Message != "")
            {
                attrs.Add(ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionMessage, ex.Message));
            }

            if (ex.ToString() != "")
            {
                attrs.Add(ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionStacktrace, ex.ToString()));
            }
        }
    }

    static void ProcessIncludedFields(LogRecord logRecord, LogEvent logEvent, IncludedData includedFields, ActivityContextCollector activityContextCollector)
    {
#if FEATURE_ACTIVITY
        if ((includedFields & (IncludedData.TraceIdField | IncludedData.SpanIdField)) != IncludedData.None)
        {
            var activityContext = activityContextCollector.GetFor(logEvent);
            
            if (activityContext is var (activityTraceId, activitySpanId))
            {
                if ((includedFields & IncludedData.TraceIdField) != IncludedData.None)
                {
                    logRecord.TraceId = ConvertUtils.ToOpenTelemetryTraceId(activityTraceId.ToHexString());
                }

                if ((includedFields & IncludedData.SpanIdField) != IncludedData.None)
                {
                    logRecord.SpanId = ConvertUtils.ToOpenTelemetrySpanId(activitySpanId.ToHexString());
                }
            }
        }
#endif

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(ConvertUtils.NewAttribute(AttributeMessageTemplateText, new()
            {
                StringValue = logEvent.MessageTemplate.Text
            }));
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(ConvertUtils.NewAttribute(AttributeMessageTemplateMD5Hash, new()
            {
                StringValue = ConvertUtils.Md5Hash(logEvent.MessageTemplate.Text)
            }));
        }
    }
}
