# Serilog.Sinks.OpenTelemetry [![Build status](https://ci.appveyor.com/api/projects/status/sqmrvw34pcuatwl5/branch/dev?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-opentelemetry/branch/dev) [![NuGet Version](http://img.shields.io/nuget/vpre/Serilog.Sinks.OpenTelemetry.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetry/)

This Serilog sink will transform Serilog events into OpenTelemetry
`LogRecord`s and send them as a protobuf payload to an OpenTelemetry
OTLP (gRPC or HTTP) endpoint.

OpenTelemetry supports attributes with scalar values, arrays, and maps.
Serilog does as well. Consequently, the sink does a one-to-one
mapping between Serilog properties and OpenTelemetry attributes.
There is no flattening, renaming, or other modifications done to the
properies by default.

The formatter renders the log message, which is then stored as the
body of the OpenTelemetry `LogRecord`.

## Getting started

To use the OpenTelemetry sink, first install the
[NuGet package](https://nuget.org/packages/serilog.sinks.opentelemetry):

```shell
dotnet add package Serilog.Sinks.OpenTelemetry
```

Then enable the sink using `WriteTo.OpenTelemetry()`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
```

Generate logs using the `Log.Information(...)` and similar methods to
send transformed logs to a local OpenTelemetry OTLP endpoint.

A more complete configuration would specify `Endpoint`, `Protocol`,
and other parameters, such as`ResourceAttributes`, as shown in the
examples below.

## Configuration

This sink supports two configuration styles: inline and options. The
inline configuration looks like:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(
        endpoint: "http://127.0.0.1:4317/v1/logs",
        protocol: OtlpProtocol.GrpcProtobuf)
    .CreateLogger();
```

This configuration is appropriate only for simple, local logging 
setups.

More complicated use cases will need to use the options
configuration, which looks like:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = endpoint;
        options.Protocol = protocol;
    })
    .CreateLogger();

```

This supports the sink's full set of configuration options. See the
`OpenTelemetrySinkOptions.cs` file for the full set of options. 
Some of the more imporant parameters are discussed in the following
sections.

### Endpoint and protocol

The default endpoint is `http://localhost:4317/v1/logs`, which will send
logs formatted as a protobuf payload to an OpenTelemetry collector
running on the same machine over the gRPC protocol. This is appropriate
for testing or for using a local OpenTelemetry collector as a proxy for
a downstream logging service.

In most production scenarios, you will want to set an endpoint. To do so,
add the `endpoint` argument to the `WriteTo.OpenTelemetry()` call. This
must be the **full** URL to an OTLP/gRPC endpoint.

You may also want to set the protocol explicitly. The supported values
are:

- `OtlpProtocol.GrpcProtobuf`: Sends a protobuf representation of the 
   OpenTelemetry Logs over a gRPC connection.
- `OtlpProtocol.HttpProtobuf`: Sends a protobuf representation of the
   OpenTelemetry Logs over an HTTP connection.

Sending OpenTelemetry logs as a JSON payload is not currently supported. 

### Resource attributes

OpenTelemetry logs may contain a "resource" that provides metadata concerning
the entity associated with the logs, typically a service or library. These
may contain "resource attributes" and are emitted for all logs flowing through
the configured logger.

These resource attributes may be provided as a `Dictionary<string, Object>`
when configuring a logger. OpenTelemetry allows resource attributes
with rich values; however, this implementation _only_ supports resource 
attributes with primitive values. 

> :warning: Resource attributes with non-primitive values will be
> silently ignored.

This example shows how the resource attributes can be specified when
the logger is configured.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://127.0.0.1:4317/v1/logs";
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "test-logging-service",
            ["index"] = 10,
            ["flag"] = true,
            ["value"] = 3.14
        };
    })
    .CreateLogger();
```

## Serilog `LogEvent` to OpenTelemetry log record mapping

The following table provides the mapping between the Serilog log 
events and the OpenTelemetry log records. 

Serilog `LogEvent`               | OpenTelemetry `LogRecord`                  | Comments                                                                                      |
---------------------------------|--------------------------------------------|-----------------------------------------------------------------------------------------------| 
`Exception.GetType().ToString()` | `Attributes["exception.type"]`             |                                                                                               |
`Exception.Message`              | `Attributes["exception.message"]`          | Ignored if empty                                                                              |
`Exception.StackTrace`           | `Attributes[ "exception.stacktrace"]`      | Value of `ex.ToString()`                                                                      |
`Level`                          | `SeverityNumber`                           | Serilog levels are mapped to corresponding OpenTelemetry severities                           | 
`Level.ToString()`               | `SeverityText`                             |                                                                                               |
`Message`                        | `Body`                                     | Culture-specific formatting can be provided via sink configuration                            |
`MessageTemplate`                | `Attributes[ "message_template.text"]`     | Requires `IncludedData. MessageTemplateText` (enabled by default)                             |
`MessageTemplate` (MD5)          | `Attributes[ "message_template.hash.md5"]` | Requires `IncludedData. MessageTemplateMD5 HashAttribute`                                     |
`Properties`                     | `Attributes`                               | Each property is mapped to an attribute keeping the name; the value's structure is maintained |
`SpanId` (`Activity.Current`)    | `SpanId`                                   | Requires `IncludedData.SpanId` (enabled by default)                                           |
`Timestamp`                      | `TimeUnixNano`                             | .NET provides 100-nanosecond precision                                                        |
`TraceId` (`Activity.Current`)   | `TraceId`                                  | Requires `IncludedData.TraceId` (enabled by default)                                          |

### Configuring included data

This sink supports configuration of how common OpenTelemetry fields are populated from
the Serilog `LogEvent` and .NET `Activity` context via the `IncludedData` flags enum:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://127.0.0.1:4317/v1/logs";
        options.IncludedData: IncludedData.MessageTemplate |
                              IncludedData.TraceId | IncludedData.SpanId;
    })
    .CreateLogger();
```

The example shows the default value; `IncludedData.MessageTemplateMD5HashAttribute` can
also be used to add the MD5 hash of the message template.

## Example

The `example/Example` subdirectory contains an example application that logs
to a local [OpenTelemetry collector](https://opentelemetry.io/docs/collector/).
See the README in that directory for instructions on how to run the example.

_Copyright &copy; Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
