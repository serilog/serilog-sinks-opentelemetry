using Xunit;
using Serilog.Collections;

namespace Serilog.Sinks.OpenTelemetry.Tests;

public class EnumerableExtensionsTests
{
    [Fact]
    public void AddToCopiesSourceToDestination()
    {
        int[] source = [1, 2, 3];
        List<int> dest = [9, 10];
        source.AddTo(dest);
        Assert.Equal([9, 10, 1, 2, 3], dest);
    }
}