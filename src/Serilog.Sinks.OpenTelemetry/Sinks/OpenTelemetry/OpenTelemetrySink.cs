﻿// Copyright 2022 Serilog Contributors
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

class OpenTelemetrySink : IBatchedLogEventSink, ILogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;
    readonly ExportLogsServiceRequest _requestTemplate;
    readonly IExporter _exporter;
    readonly IncludedData _includedData;
    readonly ActivityContextCollector _activityContextCollector;

    public OpenTelemetrySink(
       string endpoint,
       OtlpProtocol protocol,
       IFormatProvider? formatProvider,
       IDictionary<string, object>? resourceAttributes,
       IDictionary<string, string>? headers,
       IncludedData includedData,
       Func<HttpClient> httpClientFactory, 
       ActivityContextCollector activityContextCollector) 
    {
        _exporter = protocol switch
        {
            OtlpProtocol.HttpProtobuf => new HttpExporter(endpoint, headers, httpClientFactory),
            OtlpProtocol.GrpcProtobuf => new GrpcExporter(endpoint, headers, httpClientFactory),
            _ => throw new NotSupportedException($"OTLP protocol {protocol} is unsupported.")
        };

        _formatProvider = formatProvider;
        _includedData = includedData;
        _activityContextCollector = activityContextCollector;
        _requestTemplate = OpenTelemetryUtils.CreateRequestTemplate(resourceAttributes);
    }

    /// <summary>
    /// Frees any resources allocated by the IExporter.
    /// </summary>
    public void Dispose()
    {
        (_exporter as IDisposable)?.Dispose();
    }

    Task Export(ExportLogsServiceRequest request)
    {
        return _exporter.Export(request);
    }

    void AddLogEventToRequest(LogEvent logEvent, ExportLogsServiceRequest request)
    {
        var logRecord = LogRecordFactory.ToLogRecord(logEvent, _formatProvider, _includedData, _activityContextCollector);
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
        return Task.CompletedTask;
    }
}
