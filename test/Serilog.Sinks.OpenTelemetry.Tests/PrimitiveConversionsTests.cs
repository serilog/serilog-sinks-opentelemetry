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
    static byte[] GetRandomBytes(int size) {
        var bytes = new byte[size];
        var rnd = new Random();
        rnd.NextBytes(bytes);
        return bytes;
    }

    static string ByteArrayToString(byte[] bytes)
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

    [Theory]
    [InlineData(LogEventLevel.Verbose, SeverityNumber.Trace)]
    [InlineData(LogEventLevel.Debug, SeverityNumber.Debug)]
    [InlineData(LogEventLevel.Information, SeverityNumber.Info)]
    [InlineData(LogEventLevel.Warning, SeverityNumber.Warn)]
    [InlineData(LogEventLevel.Error, SeverityNumber.Error)]
    [InlineData(LogEventLevel.Fatal, SeverityNumber.Fatal)]
    public void TestToSeverityNumber(LogEventLevel level, Enum expectedSeverityNumber)
    {
        Assert.Equal((SeverityNumber)expectedSeverityNumber, PrimitiveConversions.ToSeverityNumber(level));
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

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void RejectsInvalidTraceAndSpanIds(string input)
    {
        Assert.Null(PrimitiveConversions.ToOpenTelemetryTraceId(input));
        Assert.Null(PrimitiveConversions.ToOpenTelemetrySpanId(input));
    }

    [Fact]
    public void ConstructsNewAttribute()
    {
        const string key = "ok";
        var value = new AnyValue
        {
            IntValue = 123
        };
        var attribute = PrimitiveConversions.NewAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value);
    }

    [Fact]
    public void ConstructsNewStringAttribute()
    {
        const string key = "ok";
        const string value = "also-ok";
        var attribute = PrimitiveConversions.NewStringAttribute(key, value);

        Assert.Equal(key, attribute.Key);
        Assert.Equal(value, attribute.Value?.StringValue);
    }

    [Fact]
    public void TestToOpenTelemetryScalar()
    {
        var scalar = new ScalarValue((short)100);
        var result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue(100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue((long)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue((ushort)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue((uint)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue((ulong)100);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(100, result.IntValue);

        scalar = new ScalarValue((float)3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((float)3.14, result.DoubleValue);

        scalar = new ScalarValue(3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal(3.14, result.DoubleValue);

        scalar = new ScalarValue((decimal)3.14);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double)(decimal)3.14, result.DoubleValue);

        scalar = new ScalarValue("ok");
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.Equal("ok", result.StringValue);

        scalar = new ScalarValue(true);
        result = PrimitiveConversions.ToOpenTelemetryScalar(scalar);
        Assert.True(result.BoolValue);

        // indirect conversion
        scalar = new ScalarValue(true);
        result = PrimitiveConversions.ToOpenTelemetryAnyValue(scalar, IncludedData.None);
        Assert.True(result.BoolValue);
    }

    [Fact]
    public void TestToOpenTelemetryMap()
    {
        var input = new StructureValue(
        [
            new("a", new ScalarValue(1)),
            new("b", new ScalarValue("2")),
            new("c", new ScalarValue(true))
        ], "Test");

        // direct conversion
        AssertEquivalentToInput(PrimitiveConversions.ToOpenTelemetryMap(input, IncludedData.StructureValueTypeTags));

        // indirect conversion
        AssertEquivalentToInput(PrimitiveConversions.ToOpenTelemetryAnyValue(input, IncludedData.StructureValueTypeTags));

        // no type tag
        AssertEquivalentToInput(PrimitiveConversions.ToOpenTelemetryMap(input, IncludedData.None), noTypeTag: true);
        
        return;
        
        static void AssertEquivalentToInput(AnyValue result, bool noTypeTag = false)
        {
            Assert.NotNull(result);
            var values = new Queue<KeyValue>(result.KvlistValue.Values);
            Assert.Equal(noTypeTag ? 3 : 4, values.Count);

            if (!noTypeTag)
            {
                var type = values.Dequeue();
                Assert.Equal("$type", type.Key);
                Assert.Equal("Test", type.Value.StringValue);
            }

            var a = values.Dequeue();
            Assert.Equal("a", a.Key);
            Assert.Equal(1, a.Value.IntValue);

            var b = values.Dequeue();
            Assert.Equal("b", b.Key);
            Assert.Equal("2", b.Value.StringValue);

            var c = values.Dequeue();
            Assert.Equal("c", c.Key);
            Assert.True(c.Value.BoolValue);
        }
    }

    [Fact]
    public void TestToOpenTelemetryArray()
    {
        List<LogEventPropertyValue> elements =
        [
            new ScalarValue(1),
            new ScalarValue("2"),
            new ScalarValue(false)
        ];

        var input = new SequenceValue(elements);

        // direct conversion
        var result = PrimitiveConversions.ToOpenTelemetryArray(input, IncludedData.None);
        Assert.NotNull(result);
        var arrayValue = result.ArrayValue;
        Assert.Equal(3, arrayValue?.Values.Count);
        var secondElement = arrayValue?.Values.ElementAt<AnyValue>(1);
        Assert.Equal("2", secondElement?.StringValue);

        // indirect conversion
        result = PrimitiveConversions.ToOpenTelemetryAnyValue(input, IncludedData.None);
        Assert.NotNull(result);
        arrayValue = result.ArrayValue;
        Assert.Equal(3, arrayValue?.Values.Count);
        secondElement = arrayValue?.Values.ElementAt<AnyValue>(1);
        Assert.Equal("2", secondElement?.StringValue);
    }

    [Theory]
    [InlineData("0123456789abcdefABCDEF", "0123456789abcdefABCDEF")]
    [InlineData("\f\t 123 \t\f", "123")]
    [InlineData("wrong", "")]
    [InlineData("\"123\"", "123")]
    public void TestOnlyHexDigits(string input, string expected)
    {
        Assert.Equal(expected, PrimitiveConversions.OnlyHexDigits(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("first string")]
    [InlineData("second string")]
    public void MD5RegexMatchesMD5Chars(string input)
    {
        var md5Regex = new Regex(@"^[a-f\d]{32}$");
        Assert.Matches(md5Regex, PrimitiveConversions.Md5Hash(input));
    }


    [Fact]
    public void MD5HashIsComparable()
    {
        Assert.Equal(PrimitiveConversions.Md5Hash("alpha"), PrimitiveConversions.Md5Hash("alpha"));
        Assert.NotEqual(PrimitiveConversions.Md5Hash("alpha"), PrimitiveConversions.Md5Hash("beta"));
    }

    [Fact]
    public void DictionariesMapToMaps()
    {
        var dict = new DictionaryValue([
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(0), new ScalarValue("test"))
        ]);

        var any = PrimitiveConversions.ToOpenTelemetryAnyValue(dict, IncludedData.None);

        Assert.NotNull(any.KvlistValue);
        var value = Assert.Single(any.KvlistValue.Values);
        Assert.Equal("0", value.Key);
        Assert.Equal("test", value.Value.StringValue);
    }
    
    [Fact]
    public void StructureKeysAreDeduplicated()
    {
        var structure = new StructureValue([
            new LogEventProperty("a", new ScalarValue("test")),
            new LogEventProperty("a", new ScalarValue("test")),
            new LogEventProperty("b", new ScalarValue("test"))
        ]);

        Assert.Equal(3, structure.Properties.Count);
        
        var any = PrimitiveConversions.ToOpenTelemetryAnyValue(structure, IncludedData.None);

        Assert.Equal(2, any.KvlistValue.Values.Count);
    }
}
