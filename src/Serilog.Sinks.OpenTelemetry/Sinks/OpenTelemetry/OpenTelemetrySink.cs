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
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry.ProtocolHelpers;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.OpenTelemetry;

class OpenTelemetrySink : IBatchedLogEventSink, ILogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;
    readonly ResourceLogs _resourceLogsTemplate;
    readonly IExporter _exporter;
    readonly IncludedData _includedData;

    public OpenTelemetrySink(
       string endpoint,
       OtlpProtocol protocol,
       IFormatProvider? formatProvider,
       IReadOnlyDictionary<string, object> resourceAttributes,
       IReadOnlyDictionary<string, string> headers,
       IncludedData includedData,
       HttpMessageHandler? httpMessageHandler) 
    {
        _exporter = protocol switch
        {
            OtlpProtocol.HttpProtobuf => new HttpExporter(endpoint, headers, httpMessageHandler),
            OtlpProtocol.Grpc => new GrpcExporter(endpoint, headers, httpMessageHandler),
            _ => throw new NotSupportedException($"OTLP protocol {protocol} is unsupported.")
        };

        _formatProvider = formatProvider;
        _includedData = includedData;

        if ((includedData & IncludedData.SpecRequiredResourceAttributes) == IncludedData.SpecRequiredResourceAttributes)
        {
            resourceAttributes = RequiredResourceAttributes.AddDefaults(resourceAttributes);
        }

        _resourceLogsTemplate = RequestTemplateFactory.CreateResourceLogs(resourceAttributes);
    }

    /// <summary>
    /// Frees any resources allocated by the IExporter.
    /// </summary>
    public void Dispose()
    {
        (_exporter as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Transforms and sends the given batch of LogEvent objects
    /// to an OTLP endpoint.
    /// </summary>
    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var resourceLogs = _resourceLogsTemplate.Clone();
        
        var anonymousScope = (ScopeLogs?)null;
        var namedScopes = (Dictionary<string, ScopeLogs>?)null;

        foreach (var logEvent in batch)
        {
            var (logRecord, scopeName) = LogRecordBuilder.ToLogRecord(logEvent, _formatProvider, _includedData);
            if (scopeName == null)
            {
                if (anonymousScope == null)
                {
                    anonymousScope = RequestTemplateFactory.CreateScopeLogs(null);
                    resourceLogs.ScopeLogs.Add(anonymousScope);
                }
                
                anonymousScope.LogRecords.Add(logRecord);
            }
            else
            {
                namedScopes ??= new Dictionary<string, ScopeLogs>();
                if (!namedScopes.TryGetValue(scopeName, out var namedScope))
                {
                    namedScope = RequestTemplateFactory.CreateScopeLogs(scopeName);
                    resourceLogs.ScopeLogs.Add(namedScope);
                }
                
                namedScope.LogRecords.Add(logRecord);
            }
        }

        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);
        
        return _exporter.ExportAsync(request);
    }

    /// <summary>
    /// Transforms and sends the given LogEvent
    /// to an OTLP endpoint.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        var (logRecord, scopeName) = LogRecordBuilder.ToLogRecord(logEvent, _formatProvider, _includedData);
        var scopeLogs = RequestTemplateFactory.CreateScopeLogs(scopeName);
        scopeLogs.LogRecords.Add(logRecord);
        var resourceLogs = _resourceLogsTemplate.Clone();
        resourceLogs.ScopeLogs.Add(scopeLogs);
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);
        _exporter.Export(request);
    }

    /// <summary>
    /// A no-op for an empty batch.
    /// </summary>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }
}
