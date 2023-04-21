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

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over gRPC.
/// </summary>
sealed class GrpcExporter : IExporter, IDisposable
{
    readonly LogsService.LogsServiceClient _client;

    readonly GrpcChannel _channel;

    readonly Metadata _headers;

    /// <summary>
    /// Creates a new instance of a GrpcExporter that writes an
    /// ExportLogsServiceRequest to a gRPC endpoint.
    /// </summary>
    /// <param name="endpoint">
    /// The full OTLP endpoint to which logs are sent.
    /// </param>
    /// <param name="headers">
    /// A dictionary containing the request headers.
    /// </param>
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    public GrpcExporter(string endpoint, IDictionary<string, string>? headers,
        HttpMessageHandler? httpMessageHandler = null)
    {
        var grpcChannelOptions = new GrpcChannelOptions();
        if (httpMessageHandler != null)
        {
            grpcChannelOptions.HttpClient = new HttpClient(httpMessageHandler);
            grpcChannelOptions.DisposeHttpClient = true;
        };
        
        _channel = GrpcChannel.ForAddress(endpoint, grpcChannelOptions);
        _client = new LogsService.LogsServiceClient(_channel);
        _headers = new Metadata();
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                _headers.Add(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// Frees the gRPC channel used to send logs to the OTLP endpoint.
    /// </summary>
    public void Dispose()
    {
        _channel.Dispose();
    }

    /// <summary>
    /// Sends the given protobuf request containing OpenTelemetry logs
    /// to an gRPC/HTTP endpoint.
    /// </summary>
    Task IExporter.Export(ExportLogsServiceRequest request)
    {
        return _client.ExportAsync(request, _headers).ResponseAsync;
    }
}
