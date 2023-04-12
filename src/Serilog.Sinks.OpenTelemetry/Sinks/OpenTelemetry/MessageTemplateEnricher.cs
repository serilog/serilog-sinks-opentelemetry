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

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// This class implements the ILogEventEnricher interface for
/// Serilog enrichers. This enricher will add a property that
/// containing the string representation of the message template.
/// </summary>
public class MessageTemplateEnricher : ILogEventEnricher
{
    /// <summary>
    /// Property name for the message template MD5 hash.
    /// </summary>
    public static string MESSAGE_TEMPLATE = "serilog.message.template";

    /// <summary>
    /// Creates a new MessageTemplateEnricher instance.
    /// </summary>
    public MessageTemplateEnricher() { }

    /// <summary>
    /// Implements the `ILogEventEnricher` interface, adding the
    /// string representation of the message template.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var template = logEvent.MessageTemplate.ToString();

        AddProperty(logEvent, propertyFactory, MESSAGE_TEMPLATE, template);
    }

    static void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string propertyName, string? value)
    {
        if (value != null)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, value));
        }
    }
}
