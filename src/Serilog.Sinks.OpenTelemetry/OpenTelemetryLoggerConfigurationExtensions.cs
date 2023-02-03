﻿// Copyright 2022 Serilog Contributors
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
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using static Serilog.Sinks.OpenTelemetry.OpenTelemetrySink;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog;

/// <summary>
/// Class containing extension methods to <see cref="LoggerConfiguration"/>, configuring sinks
/// to convert Serilog LogEvents to OpenTelemetry LogRecords and to send them to an OTLP/gRPC
/// endpoint.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    /// <summary>
    /// Adds a non-durable sink that transforms Serilog log events into OpenTelemetry
    /// log records, sending them to an OTLP gRPC endpoint.
    /// </summary>
    /// <param name="sinkConfiguration">
    /// The logger configuration.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP/gRPC endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use for the logger.
    /// </param>
    /// <param name="resourceAttributes">
    /// A Dictionary&lt;string, Object&gt; containing attributes of the resource attached
    /// to the logs generated by the sink. The values must be simple primitive 
    /// values: integers, doubles, strings, or booleans. Other values will be 
    /// silently ignored. 
    /// </param>
    /// <param name="headers">
    /// A Dictionary&lt;string, string&gt; containing request headers. 
    /// </param>
    /// <param name="formatProvider">
    /// Provider for formatting and rendering log messages.
    /// </param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. Default value is
    /// <see cref="LevelAlias.Minimum"/>.
    /// </param>
    /// <param name="levelSwitch">A level switch to control the minimum level for events passed through the sink.</param>
    /// <param name="batchSizeLimit">
    /// The maximum number of log events to include in a single batch.
    /// </param>
    /// <param name="batchPeriod">
    /// The maximum delay in seconds between batches.
    /// </param>
    /// <param name="batchQueueLimit">
    /// The maximum number of batches to hold in memory.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration sinkConfiguration,
        string endpoint = "http://localhost:4317/v1/logs",
        OtlpProtocol protocol = OtlpProtocol.GrpcProtobuf,
        IDictionary<string, Object>? resourceAttributes = null,
        IDictionary<string, string>? headers = null,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null,
        int batchSizeLimit = 100,
        int batchPeriod = 2,
        int batchQueueLimit = 10000)
    {
        if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));

        var sink = new OpenTelemetrySink(
            endpoint: endpoint,
            protocol: protocol,
            formatProvider: formatProvider,
            resourceAttributes: resourceAttributes,
            headers: headers);

        var batchingOptions = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = batchSizeLimit,
            Period = TimeSpan.FromSeconds(batchPeriod),
            EagerlyEmitFirstEvent = true,
            QueueLimit = batchQueueLimit
        };

        var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

        return sinkConfiguration.Sink(batchingSink, restrictedToMinimumLevel, levelSwitch);
    }
}
