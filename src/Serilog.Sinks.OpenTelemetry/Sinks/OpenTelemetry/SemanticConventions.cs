// Copyright 2024 Serilog Contributors
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

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Well-known attribute names.
/// </summary>
/// <remarks>See also https://opentelemetry.io/docs/specs/otel/logs/semantic_conventions/.</remarks>
static class SemanticConventions
{
    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionType = "exception.type";

    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionMessage = "exception.message";

    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionStacktrace = "exception.stacktrace";

    /// <summary>
    /// A https://messagetemplates.org template, as text. For example, the string <c>Hello {Name}!</c>.
    /// </summary>
    public const string AttributeMessageTemplateText = "message_template.text";

    /// <summary>
    /// A https://messagetemplates.org template, hashed using MD5 and encoded as a 128-bit hexadecimal value.
    /// </summary>
    public const string AttributeMessageTemplateMD5Hash = "message_template.hash.md5";

    /// <summary>
    /// If any placeholders in the message template use custom format specifiers, an array containing a pre-rendered string for each such token.
    /// </summary>
    public const string AttributeMessageTemplateRenderings = "message_template.renderings";

    /// <summary>
    /// OpenTelemetry standard service name resource attribute.
    /// </summary>
    public const string AttributeServiceName = "service.name";

    /// <summary>
    /// OpenTelemetry standard SDK name attribute.
    /// </summary>
    public const string AttributeTelemetrySdkName = "telemetry.sdk.name";

    /// <summary>
    /// OpenTelemetry standard SDK language attribute.
    /// </summary>
    public const string AttributeTelemetrySdkLanguage = "telemetry.sdk.language";

    /// <summary>
    /// OpenTelemetry standard SDK version attribute.
    /// </summary>
    public const string AttributeTelemetrySdkVersion = "telemetry.sdk.version";
}
