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

using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// This class implements the ILogEventEnricher interface for 
/// Serilog enrichers. If there is an active, current activity, 
/// the Trace ID and Span ID are extracted, converted to hex 
/// string representations, and then added to the log event as 
/// traceId and spanId properties.
///
/// Although this enricher may be useful in other contexts, it is 
/// designed to work with the OpenTelemetry sink, which inserts 
/// these properties into the appropriate LogRecord fields.
/// </summary>
public class TraceIdEnricher : ILogEventEnricher
{
    public const string TRACE_ID_PROPERTY_NAME = "traceId";

    public const string SPAN_ID_PROPERTY_NAME = "spanId";

    public TraceIdEnricher() { }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.TraceId.ToHexString();
        AddProperty(logEvent, propertyFactory, TRACE_ID_PROPERTY_NAME, traceId);

        var spanId = Activity.Current?.SpanId.ToHexString();
        AddProperty(logEvent, propertyFactory, SPAN_ID_PROPERTY_NAME, spanId);
    }

    private void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string propertyName, string? value)
    {
        if (value != null)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, value));
        }
    }
}
