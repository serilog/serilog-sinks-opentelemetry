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

using Serilog.Configuration;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<BatchedOpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new BatchedOpenTelemetrySinkOptions();
        configure(options);

        var openTelemetrySink = new OpenTelemetrySink(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            headers: new Dictionary<string, string>(options.Headers),
            includedData: options.IncludedData,
            httpMessageHandler: options.HttpMessageHandler);

        var sink = new PeriodicBatchingSink(openTelemetrySink, options.BatchingOptions);

        return loggerSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
        });
    }
    
    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        Action<OpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new OpenTelemetrySinkOptions();
        
        configure(options);

        var sink = new OpenTelemetrySink(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            headers: new Dictionary<string, string>(options.Headers),
            includedData: options.IncludedData,
            httpMessageHandler: options.HttpMessageHandler);

        return loggerAuditSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }
    
    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol)
    {
        if (loggerAuditSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerAuditSinkConfiguration));

        return loggerAuditSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
        });
    }
}
