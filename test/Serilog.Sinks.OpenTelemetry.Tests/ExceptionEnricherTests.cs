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

using OpenTelemetry.Trace;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class ExceptionEnricherTests
{
    [Fact]
    public void TestNoException()
    {
        var enricher = new ExceptionEnricher();
        var factory = new StringPropertyFactory();

        var logEvent = TestUtils.CreateLogEvent();

        enricher.Enrich(logEvent, factory);

        Assert.Equal(0, logEvent.Properties.Count);
    }

    [Fact]
    public void TestOnlyType()
    {
        var enricher = new ExceptionEnricher();
        var factory = new StringPropertyFactory();

        var error = new Exception("");
        var logEvent = TestUtils.CreateLogEvent(ex: error);

        enricher.Enrich(logEvent, factory);

        var expectedType = CreateKvPair(TraceSemanticConventions.AttributeExceptionType, error.GetType().ToString());

        Assert.Equal(1, logEvent.Properties.Count);
        Assert.Contains(expectedType, logEvent.Properties);
    }

    [Fact]
    public void TestTypeAndMessage()
    {
        var enricher = new ExceptionEnricher();
        var factory = new StringPropertyFactory();

        var error = new Exception("error_message");
        var logEvent = TestUtils.CreateLogEvent(ex: error);

        enricher.Enrich(logEvent, factory);

        var expectedType = CreateKvPair(TraceSemanticConventions.AttributeExceptionType, error.GetType().ToString());
        var expectedMessage = CreateKvPair(TraceSemanticConventions.AttributeExceptionMessage, error.Message);

        Assert.Equal(2, logEvent.Properties.Count);
        Assert.Contains(expectedType, logEvent.Properties);
        Assert.Contains(expectedMessage, logEvent.Properties);
    }

    [Fact]
    public void TestExceptionEnrichment()
    {
        var error = new Exception("error_message");

        var enricher = new ExceptionEnricher();
        var factory = new StringPropertyFactory();

        try
        {
            throw error;
        }
        catch (Exception ex)
        {
            var logEvent = TestUtils.CreateLogEvent(ex: ex);

            enricher.Enrich(logEvent, factory);

            var expectedType = CreateKvPair(TraceSemanticConventions.AttributeExceptionType, error.GetType().ToString());
            var expectedMessage = CreateKvPair(TraceSemanticConventions.AttributeExceptionMessage, error.Message);

            Assert.Equal(3, logEvent.Properties.Count);
            Assert.Contains(expectedType, logEvent.Properties);
            Assert.Contains(expectedMessage, logEvent.Properties);

            Assert.NotNull(ex.StackTrace);
            if (ex.StackTrace != null)
            {
                var expectedStacktrace = CreateKvPair(TraceSemanticConventions.AttributeExceptionStacktrace, ex.StackTrace);
                Assert.Contains(expectedStacktrace, logEvent.Properties);
            }
        }
    }

    KeyValuePair<string, LogEventPropertyValue> CreateKvPair(string k, string v)
    {
        return new KeyValuePair<string, LogEventPropertyValue>(k, new ScalarValue(v));
    }

}