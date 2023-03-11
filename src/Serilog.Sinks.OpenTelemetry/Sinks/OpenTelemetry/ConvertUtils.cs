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

using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.OpenTelemetry;

internal static class ConvertUtils
{
    static readonly ulong _millisToNanos = 1000000;

    internal static ulong ToUnixNano(DateTimeOffset t)
    {
        return (ulong)t.ToUnixTimeMilliseconds() * _millisToNanos;
    }

    internal static SeverityNumber ToSeverityNumber(LogEventLevel level)
    {
        switch (level)
        {
            case LogEventLevel.Verbose:
                return SeverityNumber.Trace;
            case LogEventLevel.Debug:
                return SeverityNumber.Debug;
            case LogEventLevel.Information:
                return SeverityNumber.Info;
            case LogEventLevel.Warning:
                return SeverityNumber.Warn;
            case LogEventLevel.Error:
                return SeverityNumber.Error;
            case LogEventLevel.Fatal:
                return SeverityNumber.Fatal;
            default:
                return SeverityNumber.Unspecified;
        }
    }

    internal static ByteString? ToOpenTelemetryTraceId(string hexTraceId)
    {
        var traceIdBytes = StringToByteArray(hexTraceId);
        return traceIdBytes.Length == 16 ? ByteString.CopyFrom(traceIdBytes) : null;
    }

    internal static ByteString? ToOpenTelemetrySpanId(string hexTraceId)
    {
        var spanIdBytes = StringToByteArray(hexTraceId);
        return spanIdBytes.Length == 8 ? ByteString.CopyFrom(spanIdBytes) : null;
    }

    internal static KeyValue NewAttribute(string key, AnyValue value)
    {
        return new KeyValue
        {
            Key = key,
            Value = value
        };
    }

    internal static KeyValue NewStringAttribute(string key, string value)
    {
        return NewAttribute(key, new AnyValue
        {
            StringValue = value
        });
    }

    internal static AnyValue? ToOpenTelemetryPrimitive(object? value)
    {
        switch (value)
        {
            case short i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case int i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case long i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case ushort i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case uint i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case ulong i:
                return new AnyValue
                {
                    IntValue = (long)i
                };
            case float d:
                return new AnyValue
                {
                    DoubleValue = (double)d
                };
            case double d:
                return new AnyValue
                {
                    DoubleValue = d
                };
            case decimal d:
                return new AnyValue
                {
                    DoubleValue = (double)d
                };
            case string s:
                return new AnyValue
                {
                    StringValue = s
                };
            case bool b:
                return new AnyValue
                {
                    BoolValue = b
                };
        }
        return null;
    }

    internal static AnyValue? ToOpenTelemetryScalar(ScalarValue scalar)
    {
        return ToOpenTelemetryPrimitive(scalar?.Value);
    }

    internal static AnyValue? ToOpenTelemetryMap(StructureValue value)
    {
        var map = new AnyValue();
        var kvList = new KeyValueList();
        map.KvlistValue = kvList;
        foreach (var prop in value.Properties)
        {
            var v = ToOpenTelemetryAnyValue(prop.Value);
            if (v != null)
            {
                var kv = new KeyValue
                {
                    Key = prop.Name,
                    Value = v
                };
                kvList.Values.Add(kv);
            }
        }
        return map;

    }

    internal static AnyValue? ToOpenTelemetryArray(SequenceValue value)
    {
        var array = new AnyValue();
        var values = new ArrayValue();
        array.ArrayValue = values;
        foreach (var element in value.Elements)
        {
            var v = ToOpenTelemetryAnyValue(element);
            if (v != null)
            {
                values.Values.Add(v);
            }
        }
        return array;
    }

    internal static AnyValue? ToOpenTelemetryAnyValue(LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                return ToOpenTelemetryScalar(scalar);
            case StructureValue map:
                return ToOpenTelemetryMap(map);
            case SequenceValue array:
                return ToOpenTelemetryArray(array);
            default:
                return null;
        }
    }

    internal static string OnlyHexDigits(string s)
    {
        try
        {
            return Regex.Replace(s, @"[^0-9a-fA-F]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
        }
        catch (RegexMatchTimeoutException)
        {
            return string.Empty;
        }
    }

    internal static byte[] StringToByteArray(string s)
    {
        var hex = OnlyHexDigits(s);
        var nChars = hex.Length;
        var bytes = new byte[nChars / 2];
        for (var i = 0; i < nChars; i += 2)
            bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}
