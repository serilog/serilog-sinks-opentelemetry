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
using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class ConvertUtilsTest
{
    [Fact]
    public void TestToUnixNano()
    {
        // current time has expected value
        var t0 = DateTimeOffset.UtcNow;
        var nanos = (ulong) t0.ToUnixTimeMilliseconds()*1000000; 
        var actual = ConvertUtils.ToUnixNano(t0);
        Assert.Equal(nanos, ConvertUtils.ToUnixNano(t0));

        // later time has different (greater) value
        var t1 = DateTimeOffset.UtcNow;
        Assert.True(ConvertUtils.ToUnixNano(t1) > nanos);
    }

    [Fact]
    public void TestMd5Hash()
    {
        var md5Regex = new Regex(@"^[a-f\d]{32}$");

        var inputs = new string[] {"", "first string", "second string"};
        foreach (string input in inputs) 
        {
            Assert.Matches(md5Regex, ConvertUtils.Md5Hash(input));
        }

        Assert.Equal(ConvertUtils.Md5Hash("alpha"), ConvertUtils.Md5Hash("alpha"));
        Assert.NotEqual(ConvertUtils.Md5Hash("alpha"), ConvertUtils.Md5Hash("beta"));
    }
    
    [Fact]
    public void TestToSeverityNumber()
    {
        var data = new Dictionary<LogEventLevel, SeverityNumber>() {
            {LogEventLevel.Verbose, SeverityNumber.Trace},
            {LogEventLevel.Debug, SeverityNumber.Debug},
            {LogEventLevel.Information, SeverityNumber.Info},
            {LogEventLevel.Warning, SeverityNumber.Warn},
            {LogEventLevel.Error, SeverityNumber.Error},
            {LogEventLevel.Fatal, SeverityNumber.Fatal},
        };

        foreach ((LogEventLevel level, SeverityNumber severity) in data) 
        {
            Assert.Equal(severity, ConvertUtils.ToSeverityNumber(level));
        }
    }
    
    [Fact]
    public void TestToOpenTelemetryTraceId()
    {
        var originalTraceId = ActivityTraceId.CreateRandom();
        var originalTraceIdBytes = new byte[16];
        originalTraceId.CopyTo(new Span<byte>(originalTraceIdBytes));

        var openTelemetryTraceId = ConvertUtils.ToOpenTelemetryTraceId(originalTraceId);
        Assert.Equal(16, openTelemetryTraceId.Length);


        Assert.Equal(openTelemetryTraceId.ToByteArray(), originalTraceIdBytes);
    }
    
    [Fact]
    public void TestToOpenTelemetrySpanId()
    {
        var originalSpanId = ActivitySpanId.CreateRandom();
        var originalSpanIdBytes = new byte[8];
        originalSpanId.CopyTo(new Span<byte>(originalSpanIdBytes));

        var openTelemetrySpanId = ConvertUtils.ToOpenTelemetrySpanId(originalSpanId);
        Assert.Equal(8, openTelemetrySpanId.Length);


        Assert.Equal(openTelemetrySpanId.ToByteArray(), originalSpanIdBytes);
    }
    
    [Fact]
    public void TestNewAttribute()
    {
        var key = "ok";
        var value = new AnyValue() {
            IntValue = (long) 123
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
        var scalar = new Serilog.Events.ScalarValue((Int16) 100);
        var result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((Int32) 100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((Int64) 100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((UInt16) 100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((UInt32) 100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((UInt64) 100);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((long) 100, result?.IntValue);

        scalar = new Serilog.Events.ScalarValue((Single) 3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double) (Single) 3.14, result?.DoubleValue);

        scalar = new Serilog.Events.ScalarValue((Double) 3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double) 3.14, result?.DoubleValue);

        scalar = new Serilog.Events.ScalarValue((Decimal) 3.14);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal((double) (Decimal) 3.14, result?.DoubleValue);

        scalar = new Serilog.Events.ScalarValue("ok");
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal("ok", result?.StringValue);

        scalar = new Serilog.Events.ScalarValue(true);
        result = ConvertUtils.ToOpenTelemetryScalar(scalar);
        Assert.Equal(true, result?.BoolValue);

        // indirect conversion
        scalar = new Serilog.Events.ScalarValue(true);
        result = ConvertUtils.ToOpenTelemetryAnyValue(scalar);
        Assert.Equal(true, result?.BoolValue);
    }

   [Fact]
    public void TestToOpenTelemetryArray()
    {
        var elements = new List<LogEventPropertyValue>();
        elements.Add(new Serilog.Events.ScalarValue(1));
        elements.Add(new Serilog.Events.ScalarValue("2"));
        elements.Add(new Serilog.Events.ScalarValue(false));

        var input = new Serilog.Events.SequenceValue(elements);

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
    public void TestToOpenTelemetryMap()
    {
        var properties = new List<LogEventProperty>();
        properties.Add(new Serilog.Events.LogEventProperty("a", new Serilog.Events.ScalarValue(1)));
        properties.Add(new Serilog.Events.LogEventProperty("b", new Serilog.Events.ScalarValue("2")));
        properties.Add(new Serilog.Events.LogEventProperty("c", new Serilog.Events.ScalarValue(true)));

        var input = new Serilog.Events.StructureValue(properties);

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

}