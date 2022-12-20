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
using Serilog.Sinks.PeriodicBatching;
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Common.V1;

namespace Serilog.Sinks.OpenTelemetry;

public class OpenTelemetrySink : IBatchedLogEventSink, IDisposable
{
    private readonly IFormatProvider? _formatProvider;

    private readonly LogsService.LogsServiceClient client;

    private readonly GrpcChannel channel;

    private readonly ResourceLogs resourceLogsTemplate;

    public OpenTelemetrySink(
        String endpoint,
        IFormatProvider? formatProvider,
        IDictionary<String, Object>? resourceAttributes)
    {
        channel = GrpcChannel.ForAddress(endpoint);
        client = new LogsService.LogsServiceClient(channel);

        _formatProvider = formatProvider;

        resourceLogsTemplate = CreateResourceLogsTemplate("Serilog.Sinks.OpenTelemetry", "v0.0.0", resourceAttributes);
    }

    public void Dispose()
    {
        channel.Dispose();
    }

    public void Export(ExportLogsServiceRequest request)
    {
        var response = client.Export(request);
    }

    private ResourceLogs CreateResourceLogsTemplate(String scopeName, String scopeVersion, IDictionary<String, Object>? resourceAttributes)
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
        scope.Name = "Serilog.Sinks.OpenTelemetry";
        // FIXME: Get version from build process.
        scope.Version = "v0.0.0";
        resourceLogs.ScopeLogs.Add(scopeLogs);

        return resourceLogs;
    }

    private ExportLogsServiceRequest CreateEmptyRequest()
    {
        var request = new ExportLogsServiceRequest();
        var resourceLogs = new ResourceLogs();
        request.ResourceLogs.Add(resourceLogs);
        resourceLogs.MergeFrom(resourceLogsTemplate);

        return request;
    }

    private ExportLogsServiceRequest AddLogRecordToRequest(ExportLogsServiceRequest request, LogRecord logRecord)
    {
        try
        {
            var resourceLog = request.ResourceLogs.ElementAt(0);
            resourceLog?.ScopeLogs.ElementAt(0).LogRecords.Add(logRecord);
        }
        catch (Exception) { }

        return request;
    }

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

    public Task OnEmptyBatchAsync()
    {
        return Task.FromResult(0);
    }
}
