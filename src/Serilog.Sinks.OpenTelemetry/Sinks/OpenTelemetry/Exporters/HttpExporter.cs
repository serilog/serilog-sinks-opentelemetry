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
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Serilog.Sinks.OpenTelemetry.Exporters;

/// <summary>
/// Implements an IExporter that sends OpenTelemetry Log requests
/// over HTTP using Protobuf encoding.
/// </summary>
sealed class HttpExporter : IExporter, IDisposable
{
    readonly string? _logsEndpoint;
    readonly string? _tracesEndpoint;
    readonly HttpClient _client;

    /// <summary>
    /// Creates a new instance of an HttpExporter that writes an
    /// ExportLogsServiceRequest to a OTLP/HTTP endpoint as a
    /// protobuf payload.
    /// </summary>
    /// <param name="logsEndpoint">
    /// The full OTLP logs endpoint to which logs are sent.
    /// </param>
    /// <param name="tracesEndpoint">
    /// The full OTLP traces endpoint to which logs are sent.
    /// </param>
    /// <param name="headers">
    /// A dictionary containing the request headers.
    /// </param>
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    public HttpExporter(string? logsEndpoint, string? tracesEndpoint, IReadOnlyDictionary<string, string> headers, HttpMessageHandler? httpMessageHandler = null)
    {
        _logsEndpoint = logsEndpoint;
        _tracesEndpoint = tracesEndpoint;
        _client = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
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
        var httpRequest = CreateHttpRequestMessage(request, _logsEndpoint!);

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

    public void Export(ExportTraceServiceRequest request)
    {
        var httpRequest = CreateHttpRequestMessage(request, _tracesEndpoint!);

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
        var httpRequest = CreateHttpRequestMessage(request, _logsEndpoint!);

        // We could consider using HttpCompletionOption.ResponseHeadersRead here.
        var response = await _client.SendAsync(httpRequest);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Sends the given protobuf request containing OpenTelemetry spans
    /// to an OTLP/HTTP endpoint.
    /// </summary>
    public async Task ExportAsync(ExportTraceServiceRequest request)
    {
        var httpRequest = CreateHttpRequestMessage(request, _tracesEndpoint!);

        // We could consider using HttpCompletionOption.ResponseHeadersRead here.
        var response = await _client.SendAsync(httpRequest);

        response.EnsureSuccessStatusCode();
    }

    static HttpRequestMessage CreateHttpRequestMessage(IMessage request, string endpoint)
    {
        var dataSize = request.CalculateSize();
        var buffer = new byte[dataSize];

        request.WriteTo(buffer.AsSpan());

        var content = new ByteArrayContent(buffer, 0, dataSize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Content = content;
        return httpRequest;
    }
}
