# Example Program

This subdirectory contains an example application that will 
demonstrate the Serilog OpenTelemetry sink.

The `k8s` subdirectory contains contains the Kubernetes manifest
files that will deploy an OpenTelemetry collector locally for 
testing. This **must** be deployed before running the program.

## OpenTelemetry Collector 

### Prerequisites

You must have docker installed locally with a kind cluster running
within it. The Kubernetes CLI `kubectl` must also be available.

### Manifests

- `k8s/collector-configmap.yaml`: Contains the OpenTelemetry collector
  configuration. It will listen on the standard OTLP gRPC and HTTP
  ports and write all received logs to the container's stdout. **The
  collector is configured for basic authentication.** The username
  and password are "user" and "abc123", respectively.

- `k8s/collector-deployment.yaml`: Contains the deployment
  description. This deploys the standard OpenTelemetry Contrib image,
  using the given configmap as for the configuration.

### Starting Collector

From the `k8s` subdirectory, run the command `kubectl apply -f .`. You
should see two resources created. One should be a pod with a name
starting with "collector".

To make the **port visible on your local machine**, forward the
port with this command:

```sh
k port-forward collector-... 4317:4317
```

replacing "collector-..." with your actual pod name.

You should now be ready to use the console application to send logs to
that endpoint.

### Stopping Collector

When you're done, you can shut down the local collector with `kubectl
delete -f .` from the same directory where you started it.

## Running the Program

From this subdirectory, just run the command `dotnet run`. It should
start and then send logs to the collector. The program adds the 
"Authorization" header using the basic authentication scheme.

To see the log on the collector, tail the collector logs:

```sh
kubectl logs -f collector-...
```

Replace the "collector-..." with the actual pod name on your machine.
You can find this with `kubectl get pods`. You should see logs sent
every few seconds. They should look similar to the following example.

```
2022-12-11T17:20:48.537Z	info	LogsExporter	{"kind": "exporter", "data_type": "logs", "name": "logging", "#logs": 1}
2022-12-11T17:20:48.538Z	info	ResourceLog #0
Resource SchemaURL: https://opentelemetry.io/schemas/v1.13.0
Resource attributes:
     -> service.name: Str(test-logging-service)
     -> index: Int(10)
     -> flag: Bool(true)
     -> value: Double(3.14)
ScopeLogs #0
ScopeLogs SchemaURL: https://opentelemetry.io/schemas/v1.13.0
InstrumentationScope Serilog.Sinks.OpenTelemetry v0.0.0
LogRecord #0
ObservedTimestamp: 1970-01-01 00:00:00 +0000 UTC
Timestamp: 2022-12-11 17:20:48.345 +0000 UTC
SeverityText: Information
SeverityNumber: Info(9)
Body: Str(Processed { Latitude: 25, Longitude: 134 } in 034 ms.)
Attributes:
     -> Position: Map({"Latitude":25,"Longitude":134})
     -> Elapsed: Int(34)
     -> serilog.message.template: Str(Processed {@Position} in {Elapsed:000} ms.)
     -> serilog.message.template_hash: Str(ab5776f55b04172c7c4c52c0e7d2dca0)
Trace ID: 
Span ID: 
Flags: 0
	{"kind": "exporter", "data_type": "logs", "name": "logging"}
```
