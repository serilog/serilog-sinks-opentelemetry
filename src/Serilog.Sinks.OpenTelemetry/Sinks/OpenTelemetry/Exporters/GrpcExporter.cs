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

using Grpc.Core;
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Sinks.OpenTelemetry.Exporters.ExportResults;
using static Serilog.Sinks.OpenTelemetry.Exporters.ExportResults.ExportResultExtensions;

namespace Serilog.Sinks.OpenTelemetry.Exporters;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over gRPC.
/// </summary>
sealed class GrpcExporter : IExporter, IDisposable
{
    readonly GrpcChannel? _logsChannel, _tracesChannel;

    readonly LogsService.LogsServiceClient? _logsClient;
    readonly TraceService.TraceServiceClient? _tracesClient;

    readonly Metadata _headers;

    /// <summary>
    /// Creates a new instance of a GrpcExporter that writes an
    /// ExportLogsServiceRequest to a gRPC endpoint.
    /// </summary>
    /// <param name="logsEndpoint">
    /// The gRPC endpoint to which logs are sent.
    /// </param>
    /// <param name="tracesEndpoint">
    /// The gRPC endpoint to which traces are sent.
    /// </param>
    /// <param name="headers">
    /// A dictionary containing the request headers.
    /// </param>
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    public GrpcExporter(string? logsEndpoint, string? tracesEndpoint, IReadOnlyDictionary<string, string> headers,
        HttpMessageHandler? httpMessageHandler = null)
    {
        var grpcChannelOptions = new GrpcChannelOptions();
        if (httpMessageHandler != null)
        {
            grpcChannelOptions.HttpClient = new HttpClient(httpMessageHandler);
            grpcChannelOptions.DisposeHttpClient = true;
        }

        if (logsEndpoint != null)
        {
            _logsChannel = GrpcChannel.ForAddress(logsEndpoint, grpcChannelOptions);
            _logsClient = new LogsService.LogsServiceClient(_logsChannel);
        }

        if (tracesEndpoint != null)
        {
            _tracesChannel = GrpcChannel.ForAddress(tracesEndpoint, grpcChannelOptions);
            _tracesClient = new TraceService.TraceServiceClient(_logsChannel);
        }

        _headers = new Metadata();
        foreach (var header in headers)
        {
            _headers.Add(header.Key, header.Value);
        }
    }

    public void Dispose()
    {
        _logsChannel?.Dispose();
        _tracesChannel?.Dispose();
    }

    public ExportResult Export(ExportLogsServiceRequest request)
    {
        var exportAction = () => _logsClient?.Export(request, _headers);
        return exportAction.ToExportResult(LogSuccessEvaluator);
    }

    public Task<ExportResult> ExportAsync(ExportLogsServiceRequest request)
    {
        var exportAction = () => _logsClient?.ExportAsync(request, _headers).ResponseAsync
            ?? Task.FromResult(new ExportLogsServiceResponse());
        return exportAction.ToExportResult(LogSuccessEvaluator);
    }

    public ExportResult Export(ExportTraceServiceRequest request)
    {
        var exportAction = () => _tracesClient?.Export(request, _headers);
        return exportAction.ToExportResult(TraceSuccessEvaluator);
    }

    public Task<ExportResult> ExportAsync(ExportTraceServiceRequest request)
    {
        var exportAction = () => _tracesClient?.ExportAsync(request, _headers).ResponseAsync
            ?? Task.FromResult(new ExportTraceServiceResponse());
        return exportAction.ToExportResult(TraceSuccessEvaluator);
    }
}
