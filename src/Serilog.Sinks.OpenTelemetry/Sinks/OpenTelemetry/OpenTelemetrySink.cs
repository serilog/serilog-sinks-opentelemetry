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

using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System.Reflection;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements a Serilog sink that transforms LogEvent objects into
/// OpenTelemetry LogRecord objects and emits those to an OTLP
/// endpoint.
/// </summary>
public class OpenTelemetrySink : IBatchedLogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;

    readonly LogsService.LogsServiceClient _client;

    readonly GrpcChannel _channel;

    readonly ResourceLogs _resourceLogsTemplate;

    static string? GetScopeName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name;
    }

    static string? GetScopeVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }

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
        _channel = GrpcChannel.ForAddress(endpoint);
        _client = new LogsService.LogsServiceClient(_channel);

        _formatProvider = formatProvider;

        _resourceLogsTemplate = CreateResourceLogsTemplate(GetScopeName(), GetScopeVersion(), resourceAttributes);
    }

    /// <summary>
    /// Frees the gRPC channel used to send logs to the OTLP endpoint.
    /// </summary>
    public void Dispose()
    {
        _channel.Dispose();
    }

    internal void Export(ExportLogsServiceRequest request)
    {
        var response = _client.Export(request);
    }

    ResourceLogs CreateResourceLogsTemplate(string? scopeName, string? scopeVersion, IDictionary<string, Object>? resourceAttributes)
    {
        var resourceLogs = new ResourceLogs();

        var attrs = Convert.ToResourceAttributes(resourceAttributes);
        if (attrs != null)
        {
            var resource = new Resource();
            resource.Attributes.AddRange(attrs);
            resourceLogs.Resource = resource;
            resourceLogs.SchemaUrl = Convert.SCHEMA_URL;
        }

        var scopeLogs = new ScopeLogs();
        var scope = new InstrumentationScope();
        scopeLogs.Scope = scope;
        scopeLogs.SchemaUrl = Convert.SCHEMA_URL;
        if (scopeName != null)
        {
            scope.Name = scopeName;
        }
        if (scopeVersion != null)
        {
            scope.Version = scopeVersion;
        }
        resourceLogs.ScopeLogs.Add(scopeLogs);

        return resourceLogs;
    }

    ExportLogsServiceRequest CreateEmptyRequest()
    {
        var request = new ExportLogsServiceRequest();
        var resourceLogs = new ResourceLogs();
        request.ResourceLogs.Add(resourceLogs);
        resourceLogs.MergeFrom(_resourceLogsTemplate);

        return request;
    }

    ExportLogsServiceRequest AddLogRecordToRequest(ExportLogsServiceRequest request, LogRecord logRecord)
    {
        try
        {
            var resourceLog = request.ResourceLogs.ElementAt(0);
            resourceLog?.ScopeLogs.ElementAt(0).LogRecords.Add(logRecord);
        }
        catch (Exception) { }

        return request;
    }

    /// <summary>
    /// Transforms and sends the given batch of LogEvent objects
    /// to an OTLP endpoint.
    /// </summary>
    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var request = CreateEmptyRequest();

        foreach (var logEvent in batch)
        {
            var message = logEvent.RenderMessage(_formatProvider);
            var logRecord = Convert.ToLogRecord(logEvent, message);
            AddLogRecordToRequest(request, logRecord);
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
