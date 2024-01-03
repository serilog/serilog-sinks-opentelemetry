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

using System.Net.Http;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Serilog.Sinks.OpenTelemetry.Exporters;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over HTTP using Protobuf encoding.
/// </summary>
sealed class HttpExporter : IExporter, IDisposable
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
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    public HttpExporter(string endpoint, IReadOnlyDictionary<string, string> headers, HttpMessageHandler? httpMessageHandler = null)
    {
        _client = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
        _client.BaseAddress = new Uri(endpoint);
        foreach (var header in headers)
        {
            _client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public void Export(ExportLogsServiceRequest request)
    {
        var httpRequest = CreateHttpRequestMessage(request);

#if FEATURE_SYNC_HTTP_SEND
        // Used in audit mode; on later .NET platforms this can be done without the
        // risk of deadlocks.
        // FUTURE: We could consider using HttpCompletionOption.ResponseHeadersRead here, but
        // would need to investigate any potential impacts on receivers.
        var response = _client.Send(httpRequest);
#else
        // Earlier .NET: some deadlock risk here. Necessary because in audit mode,
        // exceptions need to propagate - otherwise we'd just fire-and-forget.
        // No `ConfigureAwait(false)` because this only applies to async continuations: we're
        // staying on the same thread, here.
        var response = _client.SendAsync(httpRequest).Result;
#endif
        
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Sends the given protobuf request containing OpenTelemetry logs
    /// to an OTLP/HTTP endpoint.
    /// </summary>
    public async Task ExportAsync(ExportLogsServiceRequest request)
    {
        var httpRequest = CreateHttpRequestMessage(request);

        // We could consider using HttpCompletionOption.ResponseHeadersRead here.
        var response = await _client.SendAsync(httpRequest);
        
        response.EnsureSuccessStatusCode();
    }

    static HttpRequestMessage CreateHttpRequestMessage(ExportLogsServiceRequest request)
    {
        var dataSize = request.CalculateSize();
        var buffer = new byte[dataSize];

        request.WriteTo(buffer.AsSpan());

        var content = new ByteArrayContent(buffer, 0, dataSize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "");
        httpRequest.Content = content;
        return httpRequest;
    }
}
