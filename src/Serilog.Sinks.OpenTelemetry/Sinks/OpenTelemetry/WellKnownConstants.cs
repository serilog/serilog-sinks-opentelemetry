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

using OpenTelemetry.Trace;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Names for well-known fields attached to outgoing log events.
/// </summary>
static class WellKnownConstants
{
    /// <summary>
    /// The OpenTelemetry protocol schema.
    /// </summary>
    public const string OpenTelemetrySchemaUrl = "https://opentelemetry.io/schemas/v1.13.0";
    
    /// <summary>
    /// Property name for the trace id extracted from the current activity.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/data-model/#field-traceid.</remarks>
    public const string TraceIdField = "TraceId";

    /// <summary>
    /// Property name for the span id extracted from the current activity.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/data-model/#field-traceid.</remarks>
    public const string SpanIdField = "SpanId";
    
    /// <summary>
    /// A https://messagetemplates.org template, as text. For example, the string <c>Hello {Name}!</c>.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/semantic_conventions/ and
    /// <see cref="TraceSemanticConventions"/>.</remarks>
    public const string AttributeMessageTemplateText = "message_template.text";

    /// <summary>
    /// A https://messagetemplates.org template, hashed using MD5 and encoded as a 128-bit hexadecimal value.
    /// </summary>
    /// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/semantic_conventions/ and
    /// <see cref="TraceSemanticConventions"/>.</remarks>
    public const string AttributeMessageTemplateMd5Hash = "message_template.md5_hash";
}
