using System.IO.Compression;
using System.Net.Http;
using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using Serilog.Sinks.OpenTelemetry.Exporters;
using Xunit;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class HttpExporterTests
{
    [Fact]
    public async Task NoCompressionSerializationRoundtripWorks()
    {
        IMessage message = new AnyValue { StringValue = "Hello world!" };
        var exporter = new HttpExporter(null, null, OtlpCompression.None, new Dictionary<string, string>(0));

        using HttpRequestMessage result = exporter.CreateHttpRequestMessage(message, "");

        Assert.False(result.Content!.Headers.TryGetValues("Content-Encoding", out _));
        AnyValue roundtripped = AnyValue.Parser.ParseFrom(await result.Content!.ReadAsStreamAsync());
        Assert.Equal(message, roundtripped);
    }

    [Fact]
    public async Task GzipCompressionSerializationRoundtripWorks()
    {
        IMessage message = new AnyValue { StringValue = "Hello world!" };
        var exporter = new HttpExporter(null, null, OtlpCompression.GZip, new Dictionary<string, string>(0));

        using HttpRequestMessage result = exporter.CreateHttpRequestMessage(message, "");

        Assert.Equivalent(result.Content!.Headers.GetValues("Content-Encoding"), new [] {"gzip"});
        using var decompressor = new GZipStream(await result.Content!.ReadAsStreamAsync(),
            CompressionMode.Decompress);
        AnyValue roundtripped = AnyValue.Parser.ParseFrom(decompressor);
        Assert.Equal(message, roundtripped);
    }
}

