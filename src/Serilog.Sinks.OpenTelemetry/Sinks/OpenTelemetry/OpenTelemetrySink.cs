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

using Serilog.Core;
using Serilog.Events;
using static Serilog.Sinks.OpenTelemetry.Constants;

namespace Serilog.Sinks.OpenTelemetry;

class OpenTelemetrySink : ILogEventSink, IDisposable
{
    readonly IExporter _exporter;
    readonly ILogEventSink? _logsSink, _tracesSink;

    public OpenTelemetrySink(
        IExporter exporter,
        ILogEventSink? logsSink,
        ILogEventSink? tracesSink)
    {
        _exporter = exporter;
        _logsSink = logsSink;
        _tracesSink = tracesSink;
    }

    /// <summary>
    /// Frees any resources allocated by the IExporter and the wrapped sinks.
    /// </summary>
    public void Dispose()
    {
        (_logsSink as IDisposable)?.Dispose();
        (_tracesSink as IDisposable)?.Dispose();
        (_exporter as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Transforms and sends the given LogEvent
    /// to an OTLP endpoint.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        bool? isSpan = null;
        if (_logsSink != null)
        {
            isSpan = IsSpan(logEvent);
            if (!isSpan.Value)
            {
                _logsSink.Emit(logEvent);
                return;
            }
        }

        if (_tracesSink != null)
        {
            isSpan ??= IsSpan(logEvent);
            if (isSpan.Value)
            {
                _tracesSink.Emit(logEvent);
            }
        }
    }

    static bool IsSpan(LogEvent logEvent)
    {
        return logEvent is { TraceId: not null, SpanId: not null } &&
               logEvent.Properties.TryGetValue(SpanStartTimestampPropertyName, out var sst) &&
               sst is ScalarValue { Value: DateTime };
    }
}
