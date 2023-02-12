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

using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over HTTP.
/// </summary>
public class HttpExporter : IExporter
{
    readonly HttpClient _client;

    /// <summary>
    /// Creates a new instance of an HttpExporter that writes an 
    /// ExportLogsServiceRequest to a OTLP/HTTP endpoint as a 
    /// protobuf payload.
    /// </summary>
    /// <param name="endpoint">
    /// The full OTLP endpoint to which logs are sent. 
    /// </param>
    /// <param name="headers">
    /// A dictionary containing the request headers. 
    /// </param>
    public HttpExporter(string endpoint, IDictionary<string, string>? headers)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri(endpoint);
        if (headers != null)
        {
            foreach (var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// Frees the HTTP client that sends logs to the OTLP/HTTP endpoint.
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
    }

    /// <summary>
    /// Sends the given protobuf request containing OpenTelemetry logs
    /// to an OTLP/HTTP endpoint.
    /// </summary>
    Task IExporter.Export(ExportLogsServiceRequest request)
    {
        var dataSize = request.CalculateSize();
        var buffer = new byte[dataSize];

        request.WriteTo(buffer.AsSpan());

        var content = new ByteArrayContent(buffer, 0, dataSize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "");
        httpRequest.Content = content;

        return _client.SendAsync(httpRequest);
    }
}
