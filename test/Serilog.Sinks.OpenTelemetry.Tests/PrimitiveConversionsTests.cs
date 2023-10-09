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

using System.Text.RegularExpressions;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class PrimitiveConversionsTests
{
    public static byte[] GetRandomBytes(int size) {
        var bytes = new byte[size];
        var rnd = new Random();
        rnd.NextBytes(bytes);
        return bytes;
    }

    public static string ByteArrayToString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-","").ToLower();
    }

    [Fact]
    public void TestToUnixNano()
    {
        var t = DateTimeOffset.Parse("2023-10-09T07:00:38.7998331+00:00");
        var actual = PrimitiveConversions.ToUnixNano(t);
        Assert.Equal(1696834838799833100ul, actual);
    }

    [Fact]
    public void UnixEpochTimePreservesResolution()
    {
        var tOneHundredNanos = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero).AddTicks(1);
        var actual = PrimitiveConversions.ToUnixNano(tOneHundredNanos);
        Assert.Equal(100ul, actual);
    }

    [Fact]
    public void TestToSeverityNumber()
    {
        var data = new Dictionary<LogEventLevel, SeverityNumber>
        {
            {LogEventLevel.Verbose, SeverityNumber.Trace},
            {LogEventLevel.Debug, SeverityNumber.Debug},
            {LogEventLevel.Information, SeverityNumber.Info},
            {LogEventLevel.Warning, SeverityNumber.Warn},
            {LogEventLevel.Error, SeverityNumber.Error},
            {LogEventLevel.Fatal, SeverityNumber.Fatal},
        };

        foreach (var (level, severity) in data)
        {
            Assert.Equal(severity, PrimitiveConversions.ToSeverityNumber(level));
        }
    }

    [Fact]
    public void TestToOpenTelemetryTraceId()
    {
        var originalTraceId = GetRandomBytes(16);
        var expectedBytes = new byte[16];
        originalTraceId.CopyTo(new Span<byte>(expectedBytes));
        var originalTraceIdHexString = ByteArrayToString(originalTraceId);

        var openTelemetryTraceId = PrimitiveConversions.ToOpenTelemetryTraceId(originalTraceIdHexString);

        Assert.Equal(16, openTelemetryTraceId?.Length);
        Assert.Equal(openTelemetryTraceId?.ToByteArray(), expectedBytes);

        // default format adds quotes to string values
        var scalarHexString = new ScalarValue(originalTraceIdHexString).ToString();
        var openTelemetryTraceIdFromScalar = PrimitiveConversions.ToOpenTelemetryTraceId(scalarHexString);

        Assert.Equal(16, openTelemetryTraceIdFromScalar?.Length);
        Assert.Equal(openTelemetryTraceIdFromScalar?.ToByteArray(), expectedBytes);
    }

    [Fact]
    public void TestToOpenTelemetrySpanId()
    {
        var originalSpanId = GetRandomBytes(8);
        var expectedBytes = new byte[8];
        originalSpanId.CopyTo(new Span<byte>(expectedBytes));
        var originalSpanIdHexString = ByteArrayToString(originalSpanId);

        var openTelemetrySpanId = PrimitiveConversions.ToOpenTelemetrySpanId(originalSpanIdHexString);

        Assert.Equal(8, openTelemetrySpanId?.Length);
        Assert.Equal(openTelemetrySpanId?.ToByteArray(), expectedBytes);

        // default format adds quotes to string values
        var scalarHexString = new ScalarValue(originalSpanIdHexString).ToString();
        var openTelemetrySpanIdFromScalar = PrimitiveConversions.ToOpenTelemetrySpanId(scalarHexString);

        Assert.Equal(8, openTelemetrySpanIdFromScalar?.Length);
        Assert.Equal(openTelemetrySpanIdFromScalar?.ToByteArray(), expectedBytes);
    }

    [Fact]
    public void TestToOpenTelemetryTraceIdAndSpanIdNulls()
    {
        Assert.Null(PrimitiveConversions.ToOpenTelemetryTraceId("invalid"));
        Assert.Null(PrimitiveConversions.ToOpenTelemetryTraceId(""));
        Assert.Null(PrimitiveConversions.ToOpenTelemetrySpanId("invalid"));
        Assert.Null(PrimitiveConversions.ToOpenTelemetrySpanId(""));
    }

    [Fact]
    public void TestNewAttribute()
    {
        var key = "ok";
        var value = new AnyValue
        {
            IntValue = (long)123
        };
        var attribute = PrimitiveConversions.NewAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value);
    }

    [Fact]
    public void TestNewStringAttribute()
    {
        var key = "ok";
        var value = "also-ok";
        var attribute = PrimitiveConversions.NewStringAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value?.StringValue);
    }

    [Fact]
    public void TestToOpenTelemetryScalar()
    {
        var scalar = new ScalarValue((short)100);
        var result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((int)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((long)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((ushort)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((uint)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((ulong)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((float)3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)(float)3.14, result?.DoubleValue);

        scalar = new ScalarValue((double)3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)3.14, result?.DoubleValue);

        scalar = new ScalarValue((decimal)3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)(decimal)3.14, result?.DoubleValue);

        scalar = new ScalarValue("ok");
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal("ok", result?.StringValue);

        scalar = new ScalarValue(true);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(true, result?.BoolValue);

        // indirect conversion
        scalar = new ScalarValue(true);
        result = PrimitiveConversions.ToOpenTelemetryAnyValue(scalar);
        Assert.Equal(true, result?.BoolValue);
    }

    [Fact]
    public void TestToOpenTelemetryMap()
    {
        var properties = new List<LogEventProperty>();
        properties.Add(new LogEventProperty("a", new ScalarValue(1)));
        properties.Add(new LogEventProperty("b", new ScalarValue("2")));
        properties.Add(new LogEventProperty("c", new ScalarValue(true)));

        var input = new StructureValue(properties);

        // direct conversion
        var result = PrimitiveConversions.ToOpenTelemetryMap(input);
        Assert.NotNull(result);
        var kvlistValue = result?.KvlistValue;
        Assert.Equal(3, kvlistValue?.Values.Count);
        var secondPair = kvlistValue?.Values.ElementAt<KeyValue>(1);
        Assert.Equal("b", secondPair?.Key);
        Assert.Equal("2", secondPair?.Value.StringValue);

        // indirect conversion
        result = PrimitiveConversions.ToOpenTelemetryAnyValue(input);
        Assert.NotNull(result);
        kvlistValue = result?.KvlistValue;
        Assert.Equal(3, kvlistValue?.Values.Count);
        secondPair = kvlistValue?.Values.ElementAt<KeyValue>(1);
        Assert.Equal("b", secondPair?.Key);
        Assert.Equal("2", secondPair?.Value.StringValue);
    }

    [Fact]
    public void TestToOpenTelemetryArray()
    {
        var elements = new List<LogEventPropertyValue>();
        elements.Add(new ScalarValue(1));
        elements.Add(new ScalarValue("2"));
        elements.Add(new ScalarValue(false));

        var input = new SequenceValue(elements);

        // direct conversion
        var result = PrimitiveConversions.ToOpenTelemetryArray(input);
        Assert.NotNull(result);
        var arrayValue = result?.ArrayValue;
        Assert.Equal(3, arrayValue?.Values.Count);
        var secondElement = arrayValue?.Values.ElementAt<AnyValue>(1);
        Assert.Equal("2", secondElement?.StringValue);

        // indirect conversion
        result = PrimitiveConversions.ToOpenTelemetryAnyValue(input);
        Assert.NotNull(result);
        arrayValue = result?.ArrayValue;
        Assert.Equal(3, arrayValue?.Values.Count);
        secondElement = arrayValue?.Values.ElementAt<AnyValue>(1);
        Assert.Equal("2", secondElement?.StringValue);
    }

    [Fact]
    public void TestOnlyHexDigits()
    {
        var tests = new Dictionary<string, string>
        {
            ["0123456789abcdefABCDEF"] = "0123456789abcdefABCDEF",
            ["\f\t 123 \t\f"] = "123",
            ["wrong"] = "",
            ["\"123\""] = "123",
        };

        foreach (var (input, expected) in tests)
        {
            Assert.Equal(expected, PrimitiveConversions.OnlyHexDigits(input));
        }
    }

    [Fact]
    public void TestMd5Hash()
    {
        var md5Regex = new Regex(@"^[a-f\d]{32}$");

        var inputs = new[] { "", "first string", "second string" };
        foreach (var input in inputs)
        {
            Assert.Matches(md5Regex, PrimitiveConversions.Md5Hash(input));
        }

        Assert.Equal(PrimitiveConversions.Md5Hash("alpha"), PrimitiveConversions.Md5Hash("alpha"));
        Assert.NotEqual(PrimitiveConversions.Md5Hash("alpha"), PrimitiveConversions.Md5Hash("beta"));
    }

    [Fact]
    public void DictionariesMapToMaps()
    {
        var dict = new DictionaryValue(new[]
        {
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(0), new ScalarValue("test"))
        });

        var any = PrimitiveConversions.ToOpenTelemetryAnyValue(dict);

        Assert.NotNull(any.KvlistValue);
        var value = Assert.Single(any.KvlistValue.Values);
        Assert.Equal("0", value.Key);
        Assert.Equal("test", value.Value.StringValue);
    }
    
    [Fact]
    public void StructureKeysAreDeduplicated()
    {
        var structure = new StructureValue(new[]
        {
            new LogEventProperty("a", new ScalarValue("test")),
            new LogEventProperty("a", new ScalarValue("test")),
            new LogEventProperty("b", new ScalarValue("test"))
        });

        Assert.Equal(3, structure.Properties.Count);
        
        var any = PrimitiveConversions.ToOpenTelemetryAnyValue(structure);

        Assert.Equal(2, any.KvlistValue.Values.Count);
    }
}
