// Copyright 2022 Serilog Contributors
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

namespace Serilog;

/// <summary>
/// Class containing extension methods to <see cref="LoggerConfiguration"/>, configuring an
/// enricher that extracts the current activity's trace and span IDs and then adds them
/// as properties to the log event. 
/// </summary>
public static class TraceIdLoggerConfigurationExtensions
{
    /// <summary>
    /// If there is an active, current activity, the Trace ID and Span ID
    /// are extracted and added to the log event as traceId and spanId 
    /// properties.
    ///
    /// This enricher is designed to work with the OpenTelemetry sink.
    /// </summary>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration WithTraceIdAndSpanId(
        this LoggerEnrichmentConfiguration enrichConfiguration)
    {
        if (enrichConfiguration == null) throw new ArgumentNullException(nameof(enrichConfiguration));

        return enrichConfiguration.With<TraceIdEnricher>();
    }
}
