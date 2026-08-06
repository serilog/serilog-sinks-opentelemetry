using System.Net.Http;

namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

/// <summary>
/// Models a collector that accepts requests but never responds: the send completes
/// only through cancellation, as performed by the gRPC client when a deadline expires.
/// </summary>
class UnresponsiveHttpMessageHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("Unreachable.");
    }
}
