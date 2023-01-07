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

using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements a Serilog sink that transforms LogEvent objects into
/// OpenTelemetry LogRecord objects and emits those to an OTLP
/// endpoint.
/// </summary>
public class OpenTelemetrySink : IBatchedLogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;

    readonly ExportLogsServiceRequest _requestTemplate;

    readonly IExporter _exporter;

    /// <summary>
    /// Creates a new instance of an OpenTelemetrySink.
    /// </summary>
    /// <param name="endpoint">
    /// The full OTLP endpoint to which logs are sent. 
    /// </param>
    /// <param name="formatProvider">
    /// A IFormatProvider that is used to format the message, which
    /// is written to the LogRecord body.
    /// </param>
    /// <param name="resourceAttributes">
    /// An IDictionary&lt;string, Object&gt; containing the key-value pairs
    /// to be used as resource attributes. Non-scalar values are silently
    /// ignored.
    /// </param>
    public OpenTelemetrySink(
       string endpoint,
       IFormatProvider? formatProvider,
       IDictionary<string, Object>? resourceAttributes)
    {
        _exporter = new GrpcExporter(endpoint);

        _formatProvider = formatProvider;

        _requestTemplate = OpenTelemetryUtils.CreateRequestTemplate(resourceAttributes);
    }

    /// <summary>
    /// Frees any resources allocated by the IExporter.
    /// </summary>
    public void Dispose()
    {
        _exporter.Dispose();
    }

    void Export(ExportLogsServiceRequest request)
    {
        _exporter.Export(request);
    }

    /// <summary>
    /// Transforms and sends the given batch of LogEvent objects
    /// to an OTLP endpoint.
    /// </summary>
    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var request = _requestTemplate.Clone();

        foreach (var logEvent in batch)
        {
            var message = logEvent.RenderMessage(_formatProvider);
            var logRecord = Convert.ToLogRecord(logEvent, message);
            OpenTelemetryUtils.Add(request, logRecord);
        }
        Export(request);
        return Task.FromResult(0);
    }

    /// <summary>
    /// A no-op for an empty batch.
    /// </summary>
    public Task OnEmptyBatchAsync()
    {
        return Task.FromResult(0);
    }
}
