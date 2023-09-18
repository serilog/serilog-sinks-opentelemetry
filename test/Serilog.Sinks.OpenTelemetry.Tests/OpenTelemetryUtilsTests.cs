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

using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class RequestTemplateFactoryTests
{
    [Fact]
    // Ensure that logs are not carried over from one clone of the
    // request template to another.
    public void TestNoDuplicateLogs()
    {
        var logEvent = Some.DefaultSerilogEvent();
        var logRecord = LogRecordBuilder.ToLogRecord(logEvent, null, IncludedData.None, new());

        var requestTemplate = RequestTemplateFactory.CreateRequestTemplate(new Dictionary<string, object>());

        var request = requestTemplate.Clone();

        var n = request.ResourceLogs.ElementAt(0).ScopeLogs.ElementAt(0).LogRecords.Count;
        Assert.Equal(0, n);

        request.ResourceLogs[0].ScopeLogs[0].LogRecords.Add(logRecord);

        n = request.ResourceLogs.ElementAt(0).ScopeLogs.ElementAt(0).LogRecords.Count;
        Assert.Equal(1, n);

        request = requestTemplate.Clone();
        n = request.ResourceLogs.ElementAt(0).ScopeLogs.ElementAt(0).LogRecords.Count;

        Assert.Equal(0, n);
    }
}
