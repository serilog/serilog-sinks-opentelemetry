namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

/// <summary>
/// Records the target of each request and fails the send, so that tests can
/// observe which endpoint a client selected without running a live server.
/// </summary>
class RecordingHttpMessageHandler : HttpMessageHandler
{
    public List<Uri?> RequestUris { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri);
        throw new HttpRequestException("No server here.");
    }
}
