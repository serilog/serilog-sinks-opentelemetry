﻿// Copyright © Serilog Contributors
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

using System.Diagnostics;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Items that the sink can include in emitted log records.
/// </summary>
[Flags]
public enum IncludedData
{
    /// <summary>
    /// No additional data will be included.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Include the log event's message template in <c>message_template.text</c>. For example, the string <c>Hello {Name}!</c>.
    /// </summary>
    /// <remarks>See also https://messagetemplates.org.</remarks>
    MessageTemplateTextAttribute = 1,
    
    /// <summary>
    /// Include an MD5 hash of the log event's message template as a hex-encoded string in <c>message_template.hash.md5</c>.
    /// </summary>
    /// <remarks>See also https://messagetemplates.org.</remarks>
    MessageTemplateMD5HashAttribute = 2,
    
    /// <summary>
    /// Include <c>TraceId</c> from <see cref="Activity.Current"/> as a top-level field. Supported on .NET 6.0 and greater only.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/data-model/#field-traceid.</remarks>
    TraceIdField = 4,
    
    /// <summary>
    /// Include <c>SpanId</c> from <see cref="Activity.Current"/> as a top-level field. Supported on .NET 6.0 and greater only.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/data-model/#field-spanid.</remarks>
    SpanIdField = 8
}
