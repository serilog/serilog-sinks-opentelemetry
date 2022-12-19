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

using System;
using System.Diagnostics;
using Serilog.Events;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Trace;
using Google.Protobuf.Collections;

namespace Serilog.Sinks.OpenTelemetry;

public static class Convert
{
    public static String MESSAGE_TEMPLATE = "serilog.message.template";
    public static String MESSAGE_TEMPLATE_HASH = "serilog.message.template_hash";

    public static String SCHEMA_URL = "https://opentelemetry.io/schemas/v1.13.0";

    public static RepeatedField<KeyValue> ToResourceAttributes(IDictionary<string, Object>? resourceAttributes)
    {
        var attributes = new RepeatedField<KeyValue>();
        if (resourceAttributes != null)
        {
            foreach (KeyValuePair<string, Object> entry in resourceAttributes)
            {
                var v = ConvertUtils.ToOpenTelemetryPrimitive(entry.Value);
                if (v != null)
                {
                    var kv = new KeyValue();
                    kv.Value = v;
                    kv.Key = entry.Key;
                    attributes.Add(kv);
                }
            }
        }
        return attributes;
    }

    public static LogRecord ToLogRecord(LogEvent logEvent, String? renderedMessage)
    {
        var logRecord = new LogRecord();

        ProcessProperties(logRecord, logEvent);
        ProcessTimestamp(logRecord, logEvent);
        ProcessMessage(logRecord, renderedMessage);
        ProcessMessageTemplate(logRecord, logEvent);
        ProcessLevel(logRecord, logEvent);
        ProcessException(logRecord, logEvent);

        return logRecord;
    }

    private static void ProcessMessage(LogRecord logRecord, String? renderedMessage)
    {
        if (renderedMessage != null && renderedMessage != "")
        {
            logRecord.Body = new AnyValue()
            {
                StringValue = renderedMessage
            };
        }
    }

    private static void ProcessMessageTemplate(LogRecord logRecord, LogEvent logEvent)
    {
        var attrs = logRecord.Attributes;

        var template = logEvent.MessageTemplate;
        if (template != null)
        {
            attrs.Add(ConvertUtils.NewStringAttribute(MESSAGE_TEMPLATE, template.ToString()));

            var hash = ConvertUtils.Md5Hash(template.ToString());
            if (hash != null)
            {
                attrs.Add(ConvertUtils.NewStringAttribute(MESSAGE_TEMPLATE_HASH, hash));
            }
        }
    }

    private static void ProcessLevel(LogRecord logRecord, LogEvent logEvent)
    {
        var level = logEvent.Level;
        logRecord.SeverityText = level.ToString();
        logRecord.SeverityNumber = ConvertUtils.ToSeverityNumber(level);
    }

    private static void ProcessProperties(LogRecord logRecord, LogEvent logEvent)
    {
        var properties = logEvent.Properties;
        var attrs = logRecord.Attributes;
        foreach (var (key, value) in properties)
        {
            switch (key)
            {
                case TraceIdEnricher.TRACE_ID_PROPERTY_NAME:
                    logRecord.TraceId = ConvertUtils.ToOpenTelemetryTraceId(ActivityTraceId.CreateFromString(value.ToString()));
                    continue;

                case TraceIdEnricher.SPAN_ID_PROPERTY_NAME:
                    logRecord.SpanId = ConvertUtils.ToOpenTelemetrySpanId(ActivitySpanId.CreateFromString(value.ToString()));
                    continue;

                default:
                    var v = ConvertUtils.ToOpenTelemetryAnyValue(value);
                    if (v != null)
                    {
                        attrs.Add(ConvertUtils.NewAttribute(key, v));
                    }
                    continue;
            }
        }
    }

    private static void ProcessTimestamp(LogRecord logRecord, LogEvent logEvent)
    {
        logRecord.TimeUnixNano = ConvertUtils.ToUnixNano(logEvent.Timestamp);
    }

    private static void ProcessException(LogRecord logRecord, LogEvent logEvent)
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

            if (ex.StackTrace != null && ex.StackTrace != "")
            {
                attrs.Add(ConvertUtils.NewStringAttribute(TraceSemanticConventions.AttributeExceptionStacktrace, ex.StackTrace));
            }
        }
    }
}
