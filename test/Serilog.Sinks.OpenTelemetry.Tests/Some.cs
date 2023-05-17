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
using Serilog.Parsing;

namespace Serilog.Sinks.OpenTelemetry.Tests;

static class Some
{
    static int _nextInt;
    
    public const string TestMessageTemplate = "Message template {Variable}";

    internal static LogEvent SerilogEvent(DateTimeOffset? timestamp = null, Exception? ex = null, string messageTemplate = TestMessageTemplate)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        var logEvent = new LogEvent(
            ts,
            LogEventLevel.Warning,
            ex,
            template,
            new List<LogEventProperty>{ new("Variable", new ScalarValue(42)) });

        return logEvent;
    }

    static int Int32()
    {
        return Interlocked.Increment(ref _nextInt);
    }

    public static string String()
    {
        return $"S_{Int32()}";
    }
}
