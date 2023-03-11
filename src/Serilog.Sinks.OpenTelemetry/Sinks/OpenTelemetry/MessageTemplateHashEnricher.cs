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
using System.Security.Cryptography;
using System.Text;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// This class implements the ILogEventEnricher interface for
/// Serilog enrichers. This enricher will add a property that
/// is the MD5 hash of the message template.
/// </summary>
public class MessageTemplateHashEnricher : ILogEventEnricher
{
    /// <summary>
    /// Property name for the message template MD5 hash.
    /// </summary>
    public static string MESSAGE_TEMPLATE_HASH = "serilog.message.template_hash";

    /// <summary>
    /// Creates a new MessageTemplateHashEnricher instance.
    /// </summary>
    public MessageTemplateHashEnricher() { }

    /// <summary>
    /// Implements the `ILogEventEnricher` interface, adding the MD5
    /// hash of the message template.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var template = logEvent.MessageTemplate.ToString();
        var hash = Md5Hash(template);

        AddProperty(logEvent, propertyFactory, MESSAGE_TEMPLATE_HASH, hash);
    }

    static void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string propertyName, string? value)
    {
        if (value != null)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, value));
        }
    }

    internal static string Md5Hash(string s)
    {
        using (var md5 = MD5.Create())
        {
            md5.Initialize();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            return string.Join(string.Empty, Array.ConvertAll(hash, x => x.ToString("x2")));
        }
    }

}
