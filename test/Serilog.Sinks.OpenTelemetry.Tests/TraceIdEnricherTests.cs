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

using System.Diagnostics;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class TraceIdEnricherTests
{
    [Fact]
    public void TestEnrichNullActivity()
    {
        var logEvent = TestUtils.CreateLogEvent();

        var enricher = new TraceIdEnricher();
        var factory = new StringPropertyFactory();

        var current = Activity.Current;
        Activity.Current = null;

        enricher.Enrich(logEvent, factory);

        Assert.Empty(logEvent.Properties);

        Activity.Current = current;
    }

    [Fact]
    public void TestEnrich()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("test.activity", "1.0.0");

        var logEvent = TestUtils.CreateLogEvent();

        var enricher = new TraceIdEnricher();
        var factory = new StringPropertyFactory();

        using var activity = source.StartActivity();
        Assert.NotNull(Activity.Current);
        
        if (Activity.Current != null)
        {
            var hexTraceId = Activity.Current.TraceId.ToHexString();
            var hexSpanId = Activity.Current.SpanId.ToHexString();

            var expectedTraceId = new KeyValuePair<string, LogEventPropertyValue>(WellKnownConstants.TraceIdField, new ScalarValue(hexTraceId));
            var expectedSpanId = new KeyValuePair<string, LogEventPropertyValue>(WellKnownConstants.SpanIdField, new ScalarValue(hexSpanId));

            enricher.Enrich(logEvent, factory);

            Assert.Equal(2, logEvent.Properties.Count);
            Assert.Contains(expectedTraceId, logEvent.Properties);
            Assert.Contains(expectedSpanId, logEvent.Properties);
        }
    }

}