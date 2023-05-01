# Example Program

This subdirectory contains an example application that will 
demonstrate the Serilog OpenTelemetry sink.

The `k8s` subdirectory contains the Kubernetes manifest
files that will deploy an OpenTelemetry collector locally for 
testing. This **must** be deployed before running the program.

## OpenTelemetry Collector 

### Prerequisites

You must have docker installed locally with a kind cluster running
within it. The Kubernetes CLI `kubectl` must also be available.

### Manifests

- `k8s/collector-configmap.yaml`: Contains the OpenTelemetry collector
  configuration. It will listen on the standard OTLP gRPC and HTTP
  ports and write all received logs to the container's stdout.

- `k8s/collector-deployment.yaml`: Contains the deployment
  description. This deploys the standard OpenTelemetry Contrib image,
  using the given configmap as for the configuration.

> :information_source: The collector is configured for basic 
> authentication. The username and password are "user" and 
> "abc123", respectively.

### Start Collector

From the this subdirectory, run the command `kubectl apply -f k8s`.
You should see two resources created. One should be a pod with a name
starting with "collector".

To make the **ports visible on your local machine**, forward the
ports with by running the command in a separate terminal (or in 
the background):

```sh
kubectl port-forward collector 4317:4317 4318:4318
```

You should now be ready to use the console application to send logs to
the gRPC/protobuf and HTTP/protobuf endpoints.

### Stop Collector

When you're done, you can shut down the local collector with `kubectl
delete -f k8s` from the same directory where you started it.

## Run the Program

From this subdirectory, just run the command `dotnet run`. It should
start and then send logs to the collector. The program adds the 
"Authorization" header using the basic authentication scheme.

To see the logs on the collector, tail the collector logs:

```sh
kubectl logs -f collector
```

There should be four logs sent; two on the gRPC/protobuf endpoint and
two on the HTTP/protobuf endpoint.

They should be similar to the following examples and may or may not
be batched.

HTTP/protobuf example logs:
```
2023-01-10T13:33:27.897Z	info	ResourceLog #0
Resource SchemaURL: https://opentelemetry.io/schemas/v1.13.0
Resource attributes:
     -> service.name: Str(test-logging-service)
     -> index: Int(10)
     -> flag: Bool(true)
     -> pi: Double(3.14)
ScopeLogs #0
ScopeLogs SchemaURL: https://opentelemetry.io/schemas/v1.13.0
InstrumentationScope Serilog.Sinks.OpenTelemetry 0.0.4.0
LogRecord #0
ObservedTimestamp: 1970-01-01 00:00:00 +0000 UTC
Timestamp: 2023-01-10 13:33:27.784 +0000 UTC
SeverityText: Information
SeverityNumber: Info(9)
Body: Str({ Latitude: 13, Longitude: 111 })
Attributes:
     -> Position: Map({"Latitude":13,"Longitude":111})
     -> protocol: Str(http/protobuf)
     -> Elapsed: Int(90)
     -> serilog.message.template: Str({@Position})
     -> serilog.message.template_hash: Str(1e4184aacdb07c947a2a851f8af10a43)
Trace ID: 77c467ca8916d12d54b8a4ecfbe10d3f
Span ID: 06bb9a94b1ec4687
Flags: 0
LogRecord #1
ObservedTimestamp: 1970-01-01 00:00:00 +0000 UTC
Timestamp: 2023-01-10 13:33:27.784 +0000 UTC
SeverityText: Error
SeverityNumber: Error(17)
Body: Str(0)
Attributes:
     -> Roll: Int(0)
     -> protocol: Str(http/protobuf)
     -> exception.type: Str(System.Exception)
     -> exception.message: Str(http/protobuf)
     -> exception.stacktrace: Str(   at SerilogSinksOpenTelemetryExample.Program.SendLogs(ILogger logger, String protocol) in /Users/loomis/Documents/code/serilog/serilog-sinks-opentelemetry/test/Serilog.Sinks.OpenTelemetry.Example/Program.cs:line 71)
     -> serilog.message.template: Str({@Roll})
     -> serilog.message.template_hash: Str(175c59a67eddf1ca6a020a2ee6919e93)
Trace ID: 77c467ca8916d12d54b8a4ecfbe10d3f
Span ID: 06bb9a94b1ec4687
Flags: 0
	{"kind": "exporter", "data_type": "logs", "name": "logging"}
```

gRPC/protobuf example logs:
```
2023-01-10T13:33:27.929Z	info	ResourceLog #0
Resource SchemaURL: https://opentelemetry.io/schemas/v1.13.0
Resource attributes:
     -> service.name: Str(test-logging-service)
     -> index: Int(10)
     -> flag: Bool(true)
     -> pi: Double(3.14)
ScopeLogs #0
ScopeLogs SchemaURL: https://opentelemetry.io/schemas/v1.13.0
InstrumentationScope Serilog.Sinks.OpenTelemetry 0.0.4.0
LogRecord #0
ObservedTimestamp: 1970-01-01 00:00:00 +0000 UTC
Timestamp: 2023-01-10 13:33:27.734 +0000 UTC
SeverityText: Information
SeverityNumber: Info(9)
Body: Str({ Latitude: -48, Longitude: 2 })
Attributes:
     -> Position: Map({"Latitude":-48,"Longitude":2})
     -> protocol: Str(grpc/protobuf)
     -> Elapsed: Int(9)
     -> serilog.message.template: Str({@Position})
     -> serilog.message.template_hash: Str(1e4184aacdb07c947a2a851f8af10a43)
Trace ID: 8933b91aefd5ee9de22be4c1b0c01052
Span ID: d809a6ccef5b691e
Flags: 0
	{"kind": "exporter", "data_type": "logs", "name": "logging"}

2023-01-10T13:33:29.877Z	info	ResourceLog #0
Resource SchemaURL: https://opentelemetry.io/schemas/v1.13.0
Resource attributes:
     -> service.name: Str(test-logging-service)
     -> index: Int(10)
     -> flag: Bool(true)
     -> pi: Double(3.14)
ScopeLogs #0
ScopeLogs SchemaURL: https://opentelemetry.io/schemas/v1.13.0
InstrumentationScope Serilog.Sinks.OpenTelemetry 0.0.4.0
LogRecord #0
ObservedTimestamp: 1970-01-01 00:00:00 +0000 UTC
Timestamp: 2023-01-10 13:33:27.772 +0000 UTC
SeverityText: Error
SeverityNumber: Error(17)
Body: Str(4)
Attributes:
     -> Roll: Int(4)
     -> protocol: Str(grpc/protobuf)
     -> exception.type: Str(System.Exception)
     -> exception.message: Str(grpc/protobuf)
     -> exception.stacktrace: Str(   at SerilogSinksOpenTelemetryExample.Program.SendLogs(ILogger logger, String protocol) in /Users/loomis/Documents/code/serilog/serilog-sinks-opentelemetry/test/Serilog.Sinks.OpenTelemetry.Example/Program.cs:line 71)
     -> serilog.message.template: Str({@Roll})
     -> serilog.message.template_hash: Str(175c59a67eddf1ca6a020a2ee6919e93)
Trace ID: 8933b91aefd5ee9de22be4c1b0c01052
Span ID: d809a6ccef5b691e
Flags: 0
	{"kind": "exporter", "data_type": "logs", "name": "logging"}
```
