# Serilog.Sinks.OpenTelemetry [![Build status](https://ci.appveyor.com/api/projects/status/sqmrvw34pcuatwl5/branch/dev?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-opentelemetry/branch/dev)

This Serilog sink will transform Serilog events into OpenTelemetry
LogRecords and send them to an OpenTelemetry gRPC endpoint.

OpenTelemetry attributes support for scalar values, arrays, and maps.
Serilog does as well. Consequently, the sink does a one-to-one
mapping between Serilog properties and OpenTelemetry attributes.
There is no flattening, renaming, or other modifications done to the
properies by default.

The formatter renders the log message, which is then stored as the
body of the OpenTelemetry LogRecord.

> :exclamation: The implementation is new but stable. All feedback
> concerning the implementation, issues, and configuration is
> welcome.

## Getting Started

To use the OpenTelemetry sink, first install the
[NuGet package](https://nuget.org/packages/serilog.sinks.opentelemetry):

```shell
dotnet add package Serilog.Sinks.OpenTelemetry
```

Then enable the sink using `WriteTo.OpenTelemetry()`:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
```

Then use the `Log.Information(...)` and similar methods to send 
transformed logs to a local OpenTelemetry (OTLP/gRPC) endpoint.

A more complete configuration would specify the `endpoint` and
`resourceAttributes`. 

### Endpoint and Protocol

The default endpoint is `http://localhost:4317/v1/logs`, which will send
logs to an OpenTelemetry collector running on the same machine over the
gRPC protocol. This is appropriate for testing or for using a local
OpenTelemetry collector as a proxy for a downstream logging service.

In most production scenarios, you will want to set an endpoint. To do so,
add the `endpoint` argument to the `WriteTo.OpenTelemetry()` call. This
must be a full URL to an OTLP/gRPC endpoint.

You may also want to set the protocol explicitly. The supported values
are:

- `OtlpProtocol.GrpcProtobuf`: Sends a protobuf representation of the 
   OpenTelemetry Logs over a gRPC connection.
- `OtlpProtocol.HttpProtobuf`: Sends a protobuf representation of the
   OpenTelemetry Logs over an HTTP connection.

Sending OpenTelemetry logs as a JSON payload is not supported. 

### Resource Attributes

OpenTelemetry logs may contain a "resource" that provides metadata concerning
the entity associated with the logs, typically a service or library. These
may contain "resource attributes" and are emitted for all logs flowing through
the configured logger.

These resource attributes may be provided as a `Dictionary<string, Object>`
when configuring a logger. While OpenTelemetry allows resource attributes
with rich values; however, this implementation _only_ supports resource 
attributes with primitive values. 

> :warning: Resource attributes with non-primitive values will be
> silently ignored.

This example shows how the resource attributes can be specified when
the logger is configured.

```csharp
var log = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.OpenTelemetry(endpoint: "http://127.0.0.1:4317/v1/logs",
    resourceAttributes: new Dictionary<String, Object>() {
            {"service.name", "test-logging-service"},
            {"index", 10},
            {"flag", true},
            {"value", 3.14}
        })
    .CreateLogger();
```

## Serilog Log Events to OpenTelemetry Logs Transformation

The following table provides the mapping between the Serilog log 
events and the OpenTelemetry logs. 

Serilog (LogEvent) | OpenTelemetry (LogRecord) | Comment |
--- | --- | --- | 
Properties | Attributes[*] | Each property is mapped to an attribute keeping the name. The value's structure is maintained. |
Timestamp | Field[`TimeUnixNano`] | Serilog provides only millisecond precision |
MessageTemplate (rendered) | Field[`Body`] | Any formatter can be provided via sink configuration |
Level | Field[`SeverityText`] | Direct copy of value |
Level | Field[`SeverityNumber`] | Serilog levels mapped into corresponding OpenTelemetry levels | 
Exception | Attribute[`exception.type`] | Value of `ex.GetType()` |
Exception | Attribute[`exception.message`] | Value of `ex.Message`, if not empty |
Exception | Attribute[`exception.stacktrace`] | Value of `ex.ToString()` |

## Message Template and Message Template Hash Enrichment

This sink provides two enrichers that will add a property with the 
message template or MD5 hash of the message template to the Serilog
LogEvent.

The `WithMessageTemplate` enricher will add the property 
`serilog.message.template` which contains a string representation 
of the message template to the LogEvent.

The `WithMessageTemplateHash` enricher will add the property 
`serilog.message.template_hash` with contains the MD5 hash of the
message template to the LogEvent.

These can be used separately, together, or not at all.  The following
example shows the configuration for these enrichers:

```csharp
var log = new LoggerConfiguration()
    .Enrich.WithMessageTemplate()
    .Enrich.WithMessageTemplateHash()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
```

If the enrichers are not used, then the associated properties are not set.

## Trace Context Enrichment

OpenTelemetry allows a TraceID and SpanID to be added to log records to 
associate a log with a particular request. Within .NET, these are
found in the current activity.

If the `WithTraceIdAndSpanId` enricher is enabled and if the logging 
call takes place within an activity, the trace ID and span ID will 
be extracted from the activity and added to the Serilog LogEvent
properties (and then into the OpenTelemetry LogRecord).

```csharp
var log = new LoggerConfiguration()
    .Enrich.WithTraceIdAndSpanId()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
```

Activity.Current | LogEvent Property | OpenTelemetry (LogRecord) | Comment |
--- | --- | --- | --- |
TraceId | traceId | Field[`traceID`] | Direct binary conversion maintains fidelity |
SpanId | spanId | Field[`spanID`] | Direct binary conversion maintains fidelity | 

Although designed to be used with the OpenTelemetry sink, the enricher
may be also useful when using other sinks.

## Example

The `test/Serilog.Sinks.OpenTelemetry.Example` subdirectory contains an 
example application that logs to a local OpenTelemetry collector. See the
README in that directory for instructions on running the example.

_Copyright &copy; Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
