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

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class ConvertUtilsTest
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
        // current time has expected value
        var t0 = DateTimeOffset.UtcNow;
        var nanos = (ulong)t0.ToUnixTimeMilliseconds() * 1000000;
        var actual = ConvertUtils.ToUnixNano(t0);
        Assert.Equal(nanos, ConvertUtils.ToUnixNano(t0));

        // later time has different (greater) value
        Thread.Sleep(1000);
        var t1 = DateTimeOffset.UtcNow;
        Assert.True(ConvertUtils.ToUnixNano(t1) > nanos);
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

        foreach ((var level, var severity) in data)
        {
            Assert.Equal(severity, ConvertUtils.ToSeverityNumber(level));
        }
    }

    [Fact]
    public void TestToOpenTelemetryTraceId()
    {
        var originalTraceId = GetRandomBytes(16);
        var expectedBytes = new byte[16];
        originalTraceId.CopyTo(new Span<byte>(expectedBytes));
        var originalTraceIdHexString = ByteArrayToString(originalTraceId);

        var openTelemetryTraceId = ConvertUtils.ToOpenTelemetryTraceId(originalTraceIdHexString);

        Assert.Equal(16, openTelemetryTraceId?.Length);
        Assert.Equal(openTelemetryTraceId?.ToByteArray(), expectedBytes);

        // default format adds quotes to string values
        var scalarHexString = new ScalarValue(originalTraceIdHexString).ToString();
        var openTelemetryTraceIdFromScalar = ConvertUtils.ToOpenTelemetryTraceId(scalarHexString);

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

        var openTelemetrySpanId = ConvertUtils.ToOpenTelemetrySpanId(originalSpanIdHexString);

        Assert.Equal(8, openTelemetrySpanId?.Length);
        Assert.Equal(openTelemetrySpanId?.ToByteArray(), expectedBytes);

        // default format adds quotes to string values
        var scalarHexString = new ScalarValue(originalSpanIdHexString).ToString();
        var openTelemetrySpanIdFromScalar = ConvertUtils.ToOpenTelemetrySpanId(scalarHexString);

        Assert.Equal(8, openTelemetrySpanIdFromScalar?.Length);
        Assert.Equal(openTelemetrySpanIdFromScalar?.ToByteArray(), expectedBytes);
    }

    [Fact]
    public void TestToOpenTelemetryTraceIdAndSpanIdNulls()
    {
        Assert.Null(ConvertUtils.ToOpenTelemetryTraceId("invalid"));
        Assert.Null(ConvertUtils.ToOpenTelemetryTraceId(""));
        Assert.Null(ConvertUtils.ToOpenTelemetrySpanId("invalid"));
        Assert.Null(ConvertUtils.ToOpenTelemetrySpanId(""));
    }

    [Fact]
    public void TestNewAttribute()
    {
        var key = "ok";
        var value = new AnyValue
        {
            IntValue = (long)123
        };
        var attribute = ConvertUtils.NewAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value);
    }

    [Fact]
    public void TestNewStringAttribute()
    {
        var key = "ok";
        var value = "also-ok";
        var attribute = ConvertUtils.NewStringAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value?.StringValue);
    }

    [Fact]
    public void TestToOpenTelemetryScalar()
    {
        var scalar = new ScalarValue((short)100);
        var result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((int)100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((long)100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((ushort)100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((uint)100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((ulong)100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long)100, result?.IntValue);

        scalar = new ScalarValue((float)3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)(float)3.14, result?.DoubleValue);

        scalar = new ScalarValue((double)3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)3.14, result?.DoubleValue);

        scalar = new ScalarValue((decimal)3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)(decimal)3.14, result?.DoubleValue);

        scalar = new ScalarValue("ok");
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal("ok", result?.StringValue);

        scalar = new ScalarValue(true);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal(true, result?.BoolValue);

        // indirect conversion
        scalar = new ScalarValue(true);
        result = ConvertUtils.ToOpenTelemetryAnyValue(scalar);
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
        var result = ConvertUtils.ToOpenTelemetryMap(input);
        Assert.NotNull(result);
        var kvlistValue = result?.KvlistValue;
        Assert.Equal(3, kvlistValue?.Values.Count);
        var secondPair = kvlistValue?.Values.ElementAt<KeyValue>(1);
        Assert.Equal("b", secondPair?.Key);
        Assert.Equal("2", secondPair?.Value.StringValue);

        // indirect conversion
        result = ConvertUtils.ToOpenTelemetryAnyValue(input);
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
        var result = ConvertUtils.ToOpenTelemetryArray(input);
        Assert.NotNull(result);
        var arrayValue = result?.ArrayValue;
        Assert.Equal(3, arrayValue?.Values.Count);
        var secondElement = arrayValue?.Values.ElementAt<AnyValue>(1);
        Assert.Equal("2", secondElement?.StringValue);

        // indirect conversion
        result = ConvertUtils.ToOpenTelemetryAnyValue(input);
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
            Assert.Equal(expected, ConvertUtils.OnlyHexDigits(input));
        }
    }

}