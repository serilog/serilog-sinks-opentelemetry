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
using OpenTelemetry.Proto.Trace.V1;
using Serilog.Events;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Serilog.Sinks.OpenTelemetry.ProtocolHelpers;

static class PrimitiveConversions
{
    static class HexUtilities
    {
        internal static char ToCharLower(int value)
        {
            value = value & 0xF;
            value = value + '0';

            if (value > '9')
            {
                value = value + ('a' - ('9' + 1)); // correct the range to get 'a' to 'f'
            }

            return (char)value;
        }

        internal static int ToDigit(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            return -1;
        }

        internal static bool IsHexChar(char c)
        {
            return ((uint)(c - '0') <= 9) || // numeric
                    ((uint)(c - 'A') <= 5) || // A-F
                    ((uint)(c - 'a') <= 5); // a-f
        }
    }

    static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ulong ToUnixNano(DateTimeOffset dateTimeOffset)
    {
        if (dateTimeOffset < UnixEpoch) throw new ArgumentOutOfRangeException(nameof(dateTimeOffset));
        var timeSinceEpoch = dateTimeOffset - UnixEpoch;
        return (ulong)timeSinceEpoch.Ticks * 100;
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

    public static Status ToStatus(LogEventLevel level)
    {
        return new Status
        {
            Code = level switch
            {
                LogEventLevel.Verbose
                    or LogEventLevel.Debug
                    or LogEventLevel.Information
                    or LogEventLevel.Warning => Status.Types.StatusCode.Ok,
                LogEventLevel.Error
                    or LogEventLevel.Fatal => Status.Types.StatusCode.Error,
                _ => Status.Types.StatusCode.Unset,
            }
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

    public static AnyValue ToOpenTelemetryPrimitive(object? value)
    {
        return value switch
        {
            null => new AnyValue(),
            short i => new AnyValue { IntValue = i },
            int i => new AnyValue { IntValue = i },
            long i => new AnyValue { IntValue = i },
            byte i => new AnyValue { IntValue = i },
            ushort i => new AnyValue { IntValue = i },
            uint i => new AnyValue { IntValue = i },
            ulong i and < long.MaxValue => new AnyValue { IntValue = (long)i },
#if FEATURE_HALF
            Half d => new AnyValue { DoubleValue = (double)d },
#endif
            float d => new AnyValue { DoubleValue = d },
            double d => new AnyValue { DoubleValue = d },
            decimal d => new AnyValue { DoubleValue = (double)d },
            string s => new AnyValue { StringValue = s },
            bool b => new AnyValue { BoolValue = b },
            DateTime dateTime => new AnyValue { StringValue = dateTime.ToString("O") },
            DateTimeOffset dateTimeOffset => new AnyValue { StringValue = dateTimeOffset.ToString("O") },
#if FEATURE_DATE_AND_TIME_ONLY
            DateOnly dateOnly => new AnyValue { StringValue = dateOnly.ToString("yyyy-MM-dd") },
            TimeOnly timeOnly => new AnyValue { StringValue = timeOnly.ToString("O") },
#endif
            // We may want to thread through the format provider that's used for message rendering, but where the
            // results are consumed by a computer system rather than by an individual user InvariantCulture is often
            // more predictable.
            IFormattable f => new AnyValue { StringValue = f.ToString(null, CultureInfo.InvariantCulture) },
            _ => new AnyValue { StringValue = value.ToString() }
        };
    }

    public static AnyValue ToOpenTelemetryScalar(ScalarValue scalar)
    {
        return ToOpenTelemetryPrimitive(scalar.Value);
    }

    public static AnyValue ToOpenTelemetryMap(StructureValue value)
    {
        var map = new AnyValue();
        var kvList = new KeyValueList();
        map.KvlistValue = kvList;

        // Per the OTLP protos, attribute keys MUST be unique.
        var seen = new HashSet<string>();

        foreach (var prop in value.Properties)
        {
            if (seen.Contains(prop.Name))
                continue;

            seen.Add(prop.Name);

            var v = ToOpenTelemetryAnyValue(prop.Value);
            var kv = new KeyValue
            {
                Key = prop.Name,
                Value = v
            };
            kvList.Values.Add(kv);
        }

        return map;
    }

    public static AnyValue ToOpenTelemetryMap(DictionaryValue value)
    {
        var map = new AnyValue();
        var kvList = new KeyValueList();
        map.KvlistValue = kvList;

        foreach (var element in value.Elements)
        {
            var k = element.Key.Value?.ToString() ?? "null";
            var v = ToOpenTelemetryAnyValue(element.Value);
            kvList.Values.Add(new KeyValue
            {
                Key = k,
                Value = v
            });
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
            values.Values.Add(v);
        }
        return array;
    }

    internal static AnyValue ToOpenTelemetryAnyValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => ToOpenTelemetryScalar(scalar),
            StructureValue structure => ToOpenTelemetryMap(structure),
            SequenceValue sequence => ToOpenTelemetryArray(sequence),
            DictionaryValue dictionary => ToOpenTelemetryMap(dictionary),
            _ => ToOpenTelemetryPrimitive(value.ToString()),
        };
    }

    internal static string OnlyHexDigits(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        // further optimization: stackalloc on small strings or ArrayPool<char> renting?
        Span<char> span = new char[s.Length];
        int position = 0;

        for (int i = 0; i < s.Length; i++) 
        {
            if (HexUtilities.IsHexChar(s[i]))
            {
                span[position++] = s[i];
            }
        }

        return span.Slice(0, position).ToString();
    }

    static byte[] StringToByteArray(string s)
    {
        var hex = OnlyHexDigits(s);
        var nChars = hex.Length;
        if (nChars % 2 != 0)
        {
            hex = $"0{hex}";
        }
        ReadOnlySpan<char> hexSpan = hex.AsSpan();

        var bytes = new byte[nChars / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            int highNibble = HexUtilities.ToDigit(hexSpan[i * 2]);
            int lowNibble = HexUtilities.ToDigit(hexSpan[i * 2 + 1]);

            bytes[i] = (byte)((highNibble << 4) | lowNibble);
        }

        return bytes;
    }

    public static readonly MD5 MD5 = MD5.Create();
    public static string Md5Hash(string s)
    {
        var hash = MD5.ComputeHash(Encoding.UTF8.GetBytes(s));

        var hexStringLen = hash.Length * 2;
        var resultChars = new char[hexStringLen];

        for (int i = 0; i < hash.Length; i++)
        {
            byte b = hash[i];
            resultChars[i * 2] = HexUtilities.ToCharLower(b >> 4);
            resultChars[i * 2 + 1] = HexUtilities.ToCharLower(b);
        }


        return new string(resultChars);
    }


    public static Span.Types.SpanKind ToOpenTelemetrySpanKind(ActivityKind kind)
    {
        return kind switch
        {
            ActivityKind.Server => Span.Types.SpanKind.Server,
            ActivityKind.Client => Span.Types.SpanKind.Client,
            ActivityKind.Producer => Span.Types.SpanKind.Producer,
            ActivityKind.Consumer => Span.Types.SpanKind.Consumer,
            _ => Span.Types.SpanKind.Internal,
        };
    }
}
