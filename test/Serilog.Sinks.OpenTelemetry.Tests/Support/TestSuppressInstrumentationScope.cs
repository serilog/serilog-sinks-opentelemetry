namespace Serilog.Sinks.OpenTelemetry.Tests.Support;

class TestSuppressInstrumentationScope: IDisposable
{
    static readonly AsyncLocal<int> Depth = new();
    bool _disposed;

    public static bool IsSuppressed => Depth.Value > 0;
    
    public static IDisposable Begin()
    {
        Depth.Value++;
        return new TestSuppressInstrumentationScope();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Depth.Value--;
    }
}
