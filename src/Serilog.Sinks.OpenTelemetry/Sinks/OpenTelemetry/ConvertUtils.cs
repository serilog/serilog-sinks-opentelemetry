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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.OpenTelemetry;

internal static class ConvertUtils
{
    private static readonly ulong millisToNanos = 1000000;

    internal static ulong ToUnixNano(DateTimeOffset t)
    {
        return ((ulong)t.ToUnixTimeMilliseconds()) * millisToNanos;
    }

    internal static string Md5Hash(string s)
    {
        using (MD5 md5 = MD5.Create())
        {
            md5.Initialize();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            return String.Join(String.Empty, Array.ConvertAll(hash, x => x.ToString("x2")));
        }
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

    internal static ByteString ToOpenTelemetryTraceId(ActivityTraceId traceId)
    {
        var traceIdBytes = new byte[16];
        traceId.CopyTo(new Span<byte>(traceIdBytes));
        return ByteString.CopyFrom(traceIdBytes);
    }

    internal static ByteString? ToOpenTelemetryTraceId(String hexTraceId)
    {
        try
        {
            var span = new ReadOnlySpan<char>(OnlyHexDigits(hexTraceId).ToCharArray());
            var traceId = ActivityTraceId.CreateFromString(span);
            return ToOpenTelemetryTraceId(traceId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static ByteString ToOpenTelemetrySpanId(ActivitySpanId spanId)
    {
        var spanIdBytes = new byte[8];
        spanId.CopyTo(new Span<byte>(spanIdBytes));
        return ByteString.CopyFrom(spanIdBytes);
    }

    internal static ByteString? ToOpenTelemetrySpanId(String hexSpanId)
    {
        try
        {
            var span = new ReadOnlySpan<char>(OnlyHexDigits(hexSpanId).ToCharArray());
            var spanId = ActivitySpanId.CreateFromString(span);
            return ToOpenTelemetrySpanId(spanId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static KeyValue NewAttribute(string key, AnyValue value)
    {
        return new KeyValue()
        {
            Key = key,
            Value = value
        };
    }

    internal static KeyValue NewStringAttribute(string key, string value)
    {
        return NewAttribute(key, new AnyValue()
        {
            StringValue = value
        });
    }

    internal static AnyValue? ToOpenTelemetryPrimitive(Object? value)
    {
        switch (value)
        {
            case System.Int16 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.Int32 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.Int64 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.UInt16 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.UInt32 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.UInt64 i:
                return new AnyValue()
                {
                    IntValue = (long)i
                };
            case System.Single d:
                return new AnyValue()
                {
                    DoubleValue = (double)d
                };
            case System.Double d:
                return new AnyValue()
                {
                    DoubleValue = d
                };
            case System.Decimal d:
                return new AnyValue()
                {
                    DoubleValue = (double)d
                };
            case System.String s:
                return new AnyValue()
                {
                    StringValue = s
                };
            case System.Boolean b:
                return new AnyValue()
                {
                    BoolValue = b
                };
        }
        return null;
    }

    internal static AnyValue? ToOpenTelemetryScalar(Serilog.Events.ScalarValue scalar)
    {
        return ToOpenTelemetryPrimitive(scalar?.Value);
    }

    internal static AnyValue? ToOpenTelemetryMap(Serilog.Events.StructureValue value)
    {
        var map = new AnyValue();
        var kvList = new KeyValueList();
        map.KvlistValue = kvList;
        foreach (LogEventProperty prop in value.Properties)
        {
            var v = ConvertUtils.ToOpenTelemetryAnyValue(prop.Value);
            if (v != null)
            {
                var kv = new KeyValue();
                kv.Key = prop.Name;
                kv.Value = v;
                kvList.Values.Add(kv);
            }
        }
        return map;

    }

    internal static AnyValue? ToOpenTelemetryArray(Serilog.Events.SequenceValue value)
    {
        var array = new AnyValue();
        var values = new ArrayValue();
        array.ArrayValue = values;
        foreach (LogEventPropertyValue element in value.Elements)
        {
            var v = ConvertUtils.ToOpenTelemetryAnyValue(element);
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
            case Serilog.Events.ScalarValue scalar:
                return ConvertUtils.ToOpenTelemetryScalar(scalar);
            case Serilog.Events.StructureValue map:
                return ConvertUtils.ToOpenTelemetryMap(map);
            case Serilog.Events.SequenceValue array:
                return ConvertUtils.ToOpenTelemetryArray(array);
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
            return String.Empty;
        }
    }
}
