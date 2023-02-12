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

using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class MessageTemplateEnricherTest
{
    [Fact]
    public void TestEnrich()
    {
        var logEvent = TestUtils.CreateLogEvent();

        var enricher = new MessageTemplateEnricher();
        var factory = new StringPropertyFactory();

        var expectedMessageTemplate = new KeyValuePair<string, LogEventPropertyValue>(MessageTemplateEnricher.MESSAGE_TEMPLATE, new ScalarValue(TestUtils.TEST_MESSAGE_TEMPLATE));

        enricher.Enrich(logEvent, factory);

        Assert.Equal(1, logEvent.Properties.Count);
        Assert.Contains(expectedMessageTemplate, logEvent.Properties);
    }
}
