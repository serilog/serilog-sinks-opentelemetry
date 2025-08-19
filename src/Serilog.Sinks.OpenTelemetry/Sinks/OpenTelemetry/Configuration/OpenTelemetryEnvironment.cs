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
    public static void Configure(BatchedOpenTelemetrySinkOptions options, Func<string, string?> getConfigurationVariable)
    {
        if (ParseProtocol(getConfigurationVariable(OpenTelemetryEnvironmentVariables.DefaultProtocol)) is { } protocol)
            options.Protocol = protocol;

        if (getConfigurationVariable(OpenTelemetryEnvironmentVariables.DefaultEndpoint) is { Length: > 1 } endpoint)
            options.Endpoint = endpoint;

        FillLogsConfigUsingSpecificationEnvVars(options, getConfigurationVariable);

        FillTracesConfigUsingSpecificationEnvVars(options, getConfigurationVariable);

        FillDictionary(
            options.Headers,
            getConfigurationVariable,
            OpenTelemetryEnvironmentVariables.DefaultHeaders);

        FillDictionary(
            options.ResourceAttributes,
            getConfigurationVariable,
            OpenTelemetryEnvironmentVariables.ResourceAttributes);

        if (getConfigurationVariable(OpenTelemetryEnvironmentVariables.ServiceName) is { Length: > 1 } serviceName)
        {
            options.ResourceAttributes[SemanticConventions.AttributeServiceName] = serviceName;
        }
    }

    static OtlpProtocol? ParseProtocol(string? protocol)
    {
        return protocol switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            "grpc" => OtlpProtocol.Grpc,
            _ => null
        };
    }

    static void FillLogsConfigUsingSpecificationEnvVars(
        BatchedOpenTelemetrySinkOptions options,
        Func<string, string?> getConfigurationVariable)
    {
        options.LogsEndpoint = getConfigurationVariable(OpenTelemetryEnvironmentVariables.LogsEndpoint);
    }

    static void FillTracesConfigUsingSpecificationEnvVars(
        BatchedOpenTelemetrySinkOptions options,
        Func<string, string?> getConfigurationVariable)
    {
        options.TracesEndpoint = getConfigurationVariable(OpenTelemetryEnvironmentVariables.TracesEndpoint);
    }

    static void FillDictionary(
        IDictionary<string, string> dictionary,
        Func<string, string?> getConfigurationVariable,
        string environmentVariableName)
    {
        var config = getConfigurationVariable(environmentVariableName);
        if (config == null) return;
        foreach (var (key, value) in BaggageFormat.DecodeBaggageString(config, environmentVariableName))
        {
            dictionary[key] = value;
        }
    }

    static void FillDictionary(
        IDictionary<string, object> dictionary,
        Func<string, string?> getConfigurationVariable,
        string environmentVariableName)
    {
        var config = getConfigurationVariable(environmentVariableName);
        if (config == null) return;
        foreach (var (key, value) in BaggageFormat.DecodeBaggageString(config, environmentVariableName))
        {
            dictionary[key] = value;
        }
    }
}