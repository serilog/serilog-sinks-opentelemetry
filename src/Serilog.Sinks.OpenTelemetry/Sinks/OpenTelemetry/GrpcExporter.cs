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

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over gRPC.
/// </summary>
public class GrpcExporter : IExporter
{
    readonly LogsService.LogsServiceClient _client;

    readonly GrpcChannel _channel;

    /// <summary>
    /// Creates a new instance of a GrpcExporter that writes an 
    /// ExportLogsServiceRequest to a gRPC endpoint.
    /// </summary>
    /// <param name="endpoint">
    /// The full OTLP endpoint to which logs are sent. 
    /// </param>
    public GrpcExporter(string endpoint)
    {
        _channel = GrpcChannel.ForAddress(endpoint);
        _client = new LogsService.LogsServiceClient(_channel);
    }

    /// <summary>
    /// Frees the gRPC channel used to send logs to the OTLP endpoint.
    /// </summary>
    public void Dispose()
    {
        _channel.Dispose();
    }
 
    /// <summary>
    /// Transforms and sends the given batch of LogEvent objects
    /// to an OTLP endpoint.
    /// </summary>
    void IExporter.Export(ExportLogsServiceRequest request)
    {
        _client.Export(request); // FIXME: Ignores response.
    }
}
