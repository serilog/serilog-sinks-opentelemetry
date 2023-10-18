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

using OpenTelemetry.Proto.Logs.V1;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class RequestTemplateFactoryTests
{
    [Fact]
    public void ResourceLogsAreClonedDeeply()
    {
        var template = RequestTemplateFactory.CreateResourceLogs(new Dictionary<string, object>());

        var request = template.Clone();

        var n = request.ScopeLogs.Count;
        Assert.Equal(0, n);

        request.ScopeLogs.Add(new ScopeLogs());

        n = request.ScopeLogs.Count;
        Assert.Equal(1, n);
        
        request = template.Clone();

        n = request.ScopeLogs.Count;
        Assert.Equal(0, n);
    }
}
