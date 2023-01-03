# Developer Guide

## Design Choices

### Direct Use of Proto Definitions

The OpenTelemetry logging sink will be used in environments that may
have severe limitations on dependency package versions. The
OpenTelemetry .NET SDK has a large number of dependencies (~50) that
would make its use difficult in these environments.

Consequently, this sink imports the C# OpenTelemetry Proto bindings
directly to minimize the dependencies. As the proto definitions
evolve, this may require support for multiple versions of the sink.

### Direct Mapping of Properties

OpenTelemetry attributes allows full support for scalar values,
arrays, and maps. Serilog does as well. Consequently, the sink does a
one-to-one mapping between Serilog properties and OpenTelemetry
attributes. There is no flattening, renaming, or other modifications
done to the properies by default.

The configured formatter (or default) renders the log message. This
message is written to the OpenTelemetry LogRecord body.

## OpenTelemetry Proto

The OpenTelemetry data structures are defined in the language-neutral
protobuf format. To generate the language-specific bindings for C#,
you must have `make` and `docker` installed.

With the necessary prerequisites available, you can then:

- Clone the
  [open-telemetry/opentelemetry-proto](https://github.com/open-telemetry/opentelemetry-proto)
  repository.

- Checkout the appropriate tag for the version of the OpenTelemetry
  Collector that you want to support.

- Run `make gen-csharp` from the root of the cloned repository.

- Copy the files in `gen/csharp` to the `src/OpenTelemetryProto` 
  directory in this repository.

- The files for metrics and traces are not necessary and can be
  removed.

Ensure that all unit tests pass before checking in the new code.
