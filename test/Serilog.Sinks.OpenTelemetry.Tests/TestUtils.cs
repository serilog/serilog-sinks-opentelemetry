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
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public static class TestUtils
{
    internal static LogEvent CreateLogEvent(DateTimeOffset? timestamp = null, Exception? ex = null)
    {
        var ts = (timestamp != null) ? (DateTimeOffset)timestamp : DateTimeOffset.UtcNow;
        var template = new MessageTemplate(new List<MessageTemplateToken>());
        var logRecord = new LogRecord();
        var logEvent = new LogEvent(
            ts,
            LogEventLevel.Warning,
            ex,
            template,
            new List<LogEventProperty>());

        return logEvent;
    }
}

public class StringPropertyFactory : Core.ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        return new LogEventProperty(name, new ScalarValue(value?.ToString()));
    }
}
