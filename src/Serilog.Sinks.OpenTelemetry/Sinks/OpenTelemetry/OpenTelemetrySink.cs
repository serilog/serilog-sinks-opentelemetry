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
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements a Serilog sink that transforms LogEvent objects into
/// OpenTelemetry LogRecord objects and emits those to an OTLP
/// endpoint.
/// </summary>
class OpenTelemetrySink : IBatchedLogEventSink, ILogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;

    readonly ExportLogsServiceRequest _requestTemplate;

    readonly IExporter _exporter;

    readonly LogRecordData _includedFields = LogRecordData.MessageTemplateTextAttribute | LogRecordData.TraceIdField |
                                             LogRecordData.SpanIdField;

    /// <summary>
    /// Creates a new instance of an OpenTelemetrySink.
    /// </summary>
    /// <param name="endpoint">
    /// The full OTLP endpoint to which logs are sent.
    /// </param>
    /// <param name="protocol">
    /// The protocol to use when sending data. Defaults to gRPC/protobuf.
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
    /// <param name="headers">
    /// An IDictionary&lt;string, string&gt; containing the key-value pairs
    /// to be used as request headers.
    /// </param>
    public OpenTelemetrySink(
       string endpoint,
       OtlpProtocol protocol = OtlpProtocol.GrpcProtobuf,
       IFormatProvider? formatProvider = null,
       IDictionary<string, object>? resourceAttributes = null,
       IDictionary<string, string>? headers = null)
    {
        switch (protocol)
        {
            case OtlpProtocol.HttpProtobuf:
                _exporter = new HttpExporter(endpoint, headers);
                break;

            default:
                _exporter = new GrpcExporter(endpoint, headers);
                break;
        }

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

    Task Export(ExportLogsServiceRequest request)
    {
        return _exporter.Export(request);
    }

    void AddLogEventToRequest(LogEvent logEvent, ExportLogsServiceRequest request)
    {
        var logRecord = LogRecordFactory.ToLogRecord(logEvent, _formatProvider, _includedFields);
        OpenTelemetryUtils.Add(request, logRecord);
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
            AddLogEventToRequest(logEvent, request);
        }

        return Export(request);
    }

    /// <summary>
    /// Transforms and sends the given LogEvent
    /// to an OTLP endpoint.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        var request = _requestTemplate.Clone();
        AddLogEventToRequest(logEvent, request);
        Export(request)
            .ContinueWith(t => SelfLog.WriteLine("Exception while emitting event from {0}: {1}", this, t.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// A no-op for an empty batch.
    /// </summary>
    public Task OnEmptyBatchAsync()
    {
        return Task.FromResult(0);
    }
}
