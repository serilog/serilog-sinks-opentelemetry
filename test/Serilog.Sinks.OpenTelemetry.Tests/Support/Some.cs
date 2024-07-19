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
using Serilog.Parsing;
// ReSharper disable UnusedMember.Global

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

static class Some
{
    static int _nextInt;
    
    public const string TestMessageTemplate = "Message template {Variable}";

    internal static LogEvent DefaultSerilogEvent()
    {
        return SerilogEvent(
            TestMessageTemplate,
            new List<LogEventProperty> { new("Variable", new ScalarValue(42)) },
            DateTimeOffset.UtcNow,
            ex: null);
    }

    internal static LogEvent SerilogEvent(string messageTemplate, DateTimeOffset? timestamp = null, Exception? ex = null)
    {
        return SerilogEvent(messageTemplate, new List<LogEventProperty>(), timestamp, ex);
    }

	internal static LogEvent SerilogEvent(string messageTemplate, IEnumerable<LogEventProperty> properties, DateTimeOffset? timestamp = null, Exception? ex = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        var logEvent = new LogEvent(
            ts,
            LogEventLevel.Warning,
            ex,
            template,
            properties);

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

    public static DateTime UtcDateTime()
    {
        return DateTime.UtcNow;
    }

    public sealed class TestActivity(ActivityListener listener, ActivitySource source, Activity activity) : IDisposable
    {
        public Activity Activity => activity;
        
        public void Dispose()
        {
            activity.Dispose();
            source.Dispose();
            listener.Dispose();
        }
    }

    public static TestActivity Activity()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource(String(), "1.0.0");

        var activity = source.StartActivity();

        return new TestActivity(listener, source, activity!);
    }
}
