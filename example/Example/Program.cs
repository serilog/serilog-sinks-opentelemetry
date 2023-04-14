using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.OpenTelemetry;

SelfLog.Enable(Console.Out);

Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://localhost:4318/v1/logs";
        options.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .CreateLogger();

try
{
    Log.Information("Hello, {Name}!", "OpenTelemetry");
}
catch (Exception ex)
{
    Log.Error(ex, "Unhandled exception");
}
finally
{
    await Log.CloseAndFlushAsync();
}
