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

using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using System.Text.RegularExpressions;
using Serilog.Debugging;

namespace Serilog.Sinks.OpenTelemetry;

static class ConvertUtils
{
    const ulong MillisToNanos = 1000000;

    public static ulong ToUnixNano(DateTimeOffset t)
    {
        return (ulong)t.ToUnixTimeMilliseconds() * MillisToNanos;
    }

    public static SeverityNumber ToSeverityNumber(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => SeverityNumber.Trace,
            LogEventLevel.Debug => SeverityNumber.Debug,
            LogEventLevel.Information => SeverityNumber.Info,
            LogEventLevel.Warning => SeverityNumber.Warn,
            LogEventLevel.Error => SeverityNumber.Error,
            LogEventLevel.Fatal => SeverityNumber.Fatal,
            _ => SeverityNumber.Unspecified
        };
    }

    public static ByteString? ToOpenTelemetryTraceId(string hexTraceId)
    {
        var traceIdBytes = StringToByteArray(hexTraceId);
        return traceIdBytes.Length == 16 ? ByteString.CopyFrom(traceIdBytes) : null;
    }

    public static ByteString? ToOpenTelemetrySpanId(string hexTraceId)
    {
        var spanIdBytes = StringToByteArray(hexTraceId);
        return spanIdBytes.Length == 8 ? ByteString.CopyFrom(spanIdBytes) : null;
    }

    public static KeyValue NewAttribute(string key, AnyValue value)
    {
        return new KeyValue
        {
            Key = key,
            Value = value
        };
    }

    public static KeyValue NewStringAttribute(string key, string value)
    {
        return NewAttribute(key, new AnyValue
        {
            StringValue = value
        });
    }

    public static AnyValue? ToOpenTelemetryPrimitive(object? value)
    {
        return value switch
        {
            short i => new AnyValue { IntValue = i },
            int i => new AnyValue { IntValue = i },
            long i => new AnyValue { IntValue = i },
            ushort i => new AnyValue { IntValue = i },
            uint i => new AnyValue { IntValue = i },
            ulong i => new AnyValue { IntValue = (long)i },
            float d => new AnyValue { DoubleValue = d },
            double d => new AnyValue { DoubleValue = d },
            decimal d => new AnyValue { DoubleValue = (double)d },
            string s => new AnyValue { StringValue = s },
            bool b => new AnyValue { BoolValue = b },
            _ => null
        };
    }

    public static AnyValue? ToOpenTelemetryScalar(ScalarValue scalar)
    {
        return ToOpenTelemetryPrimitive(scalar.Value);
    }

    public static AnyValue ToOpenTelemetryMap(StructureValue value)
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

    public static AnyValue ToOpenTelemetryArray(SequenceValue value)
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
        return value switch
        {
            ScalarValue scalar => ToOpenTelemetryScalar(scalar),
            StructureValue map => ToOpenTelemetryMap(map),
            SequenceValue array => ToOpenTelemetryArray(array),
            // Not currently supported; OpenTelemetry maps have string keys, so these will most likely become arrays of pairs.
            DictionaryValue _ => null,
            _ => null,
        };
    }

    internal static string OnlyHexDigits(string s)
    {
        try
        {
            return Regex.Replace(s, @"[^0-9a-fA-F]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
        }
        catch (RegexMatchTimeoutException)
        {
            SelfLog.WriteLine("Regular expression matching timed out over {0}", s);
            return string.Empty;
        }
    }

    static byte[] StringToByteArray(string s)
    {
        var hex = OnlyHexDigits(s);
        var nChars = hex.Length;
        var bytes = new byte[nChars / 2];
        for (var i = 0; i < nChars; i += 2)
            bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
    
    public static string Md5Hash(string s)
    {
        using var md5 = MD5.Create();
        md5.Initialize();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
        return string.Join(string.Empty, Array.ConvertAll(hash, x => x.ToString("x2")));
    }
}
