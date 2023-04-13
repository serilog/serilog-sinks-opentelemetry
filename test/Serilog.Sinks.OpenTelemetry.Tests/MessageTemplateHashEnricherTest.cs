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
using System.Text.RegularExpressions;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class MessageTemplateHashEnricherTest
{
    [Fact]
    public void TestMd5Hash()
    {
        var md5Regex = new Regex(@"^[a-f\d]{32}$");

        var inputs = new[] { "", "first string", "second string" };
        foreach (var input in inputs)
        {
            Assert.Matches(md5Regex, MessageTemplateHashEnricher.Md5Hash(input));
        }

        Assert.Equal(MessageTemplateHashEnricher.Md5Hash("alpha"), MessageTemplateHashEnricher.Md5Hash("alpha"));
        Assert.NotEqual(MessageTemplateHashEnricher.Md5Hash("alpha"), MessageTemplateHashEnricher.Md5Hash("beta"));
    }

    [Fact]
    public void TestEnrich()
    {
        var logEvent = TestUtils.CreateLogEvent();

        var enricher = new MessageTemplateHashEnricher();
        var factory = new StringPropertyFactory();

        var expectedHash = MessageTemplateHashEnricher.Md5Hash(TestUtils.TEST_MESSAGE_TEMPLATE);

        var expectedMessageTemplateHash = new KeyValuePair<string, LogEventPropertyValue>(WellKnownConstants.AttributeMessageTemplateMd5Hash, new ScalarValue(expectedHash));

        enricher.Enrich(logEvent, factory);

        Assert.Equal(1, logEvent.Properties.Count);
        Assert.Contains(expectedMessageTemplateHash, logEvent.Properties);
    }
}
