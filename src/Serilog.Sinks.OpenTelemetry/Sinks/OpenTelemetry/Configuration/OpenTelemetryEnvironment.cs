// Copyright © Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Serilog.Sinks.OpenTelemetry.Configuration;

static class OpenTelemetryEnvironment
{
    const string ProtocolVarName = "OTEL_EXPORTER_OTLP_PROTOCOL";
    const string EndpointVarName = "OTEL_EXPORTER_OTLP_ENDPOINT";
    const string HeaderVarName = "OTEL_EXPORTER_OTLP_HEADERS";
    const string ResourceAttributesVarName = "OTEL_RESOURCE_ATTRIBUTES";
    const string ServiceNameVarName = "OTEL_SERVICE_NAME";

    public static void Configure(BatchedOpenTelemetrySinkOptions options, Func<string, string?> getConfigurationVariable)
    {
        options.Protocol = getConfigurationVariable(ProtocolVarName) switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => options.Protocol
        };

        if (getConfigurationVariable(EndpointVarName) is { Length: > 1 } endpoint)
            options.Endpoint = endpoint;

        FillHeadersIfPresent(getConfigurationVariable(HeaderVarName), options.Headers);

        FillHeadersResourceAttributesIfPresent(getConfigurationVariable(ResourceAttributesVarName), options.ResourceAttributes);

        if (getConfigurationVariable(ServiceNameVarName) is { Length: > 1 } serviceName)
        {
            options.ResourceAttributes[SemanticConventions.AttributeServiceName] = serviceName;
        }
    }

    static void FillHeadersIfPresent(string? config, IDictionary<string, string> headers)
    {
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                headers[parts[0]] = parts[1];
            else
                throw new InvalidOperationException($"Invalid header format `{part}` in {HeaderVarName} environment variable.");
        }
    }

    static void FillHeadersResourceAttributesIfPresent(string? config, IDictionary<string, object> resourceAttributes)
    {
        foreach (var part in config?.Split(',') ?? [])
        {
            if (part.Split('=') is { Length: 2 } parts)
                resourceAttributes[parts[0]] = parts[1];
            else
                throw new InvalidOperationException($"Invalid resource attributes format `{part}` in {ResourceAttributesVarName} environment variable.");
        }
    }
}
