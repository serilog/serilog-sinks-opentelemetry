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
using Serilog.Core;
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
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<OpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new OpenTelemetrySinkOptions();
        configure(options);

        var collector = new ActivityContextCollector();
        
        var openTelemetrySink = new OpenTelemetrySink(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            formatProvider: options.FormatProvider,
            resourceAttributes: options.ResourceAttributes,
            headers: options.Headers,
            includedData: options.IncludedData,
            httpMessageHandler: options.HttpMessageHandler,
            activityContextCollector: collector);

        ILogEventSink sink = openTelemetrySink;
        if (options.BatchingOptions != null)
        {
            sink = new PeriodicBatchingSink(openTelemetrySink, options.BatchingOptions);
        }

        sink = new ActivityContextCollectorSink(collector, sink);
        
        return loggerSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The logger configuration.
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
}
