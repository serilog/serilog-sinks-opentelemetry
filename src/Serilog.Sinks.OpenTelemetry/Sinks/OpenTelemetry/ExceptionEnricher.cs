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

using OpenTelemetry.Trace;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// This class implements the ILogEventEnricher interface for 
/// Serilog enrichers. If there is an exception in the LogEvent
/// then the enricher will extract the exception type, message,
/// and stacktrace (as available) and put these into the 
/// properties `exception.type`, `exception.message`, and 
/// `exception.stacktrace` properties from the OpenTelemetry
/// semantic conventions.
/// </summary>
public class ExceptionEnricher : ILogEventEnricher
{
    /// <summary>
    /// Creates a new ExceptionEnricher instance.
    /// </summary>
    public ExceptionEnricher() { }

    /// <summary>
    /// Implements the `ILogEventEnricher` interface, adding `exception.type`,
    /// `exception.message`, and `exception.stacktrace` properties. Properties
    /// are added only if the values are not null and not the empty string.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var ex = logEvent.Exception;
        AddProperty(logEvent, propertyFactory, TraceSemanticConventions.AttributeExceptionType, ex?.GetType().ToString());
        AddProperty(logEvent, propertyFactory, TraceSemanticConventions.AttributeExceptionMessage, ex?.Message);
        AddProperty(logEvent, propertyFactory, TraceSemanticConventions.AttributeExceptionStacktrace, ex?.StackTrace);
    }

    void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string propertyName, string? value)
    {
        if (value != null && value != "")
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, value));
        }
    }
}
