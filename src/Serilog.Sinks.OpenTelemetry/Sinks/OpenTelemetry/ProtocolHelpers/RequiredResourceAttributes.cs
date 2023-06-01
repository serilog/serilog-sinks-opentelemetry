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

using System.Diagnostics;

namespace Serilog.Sinks.OpenTelemetry.ProtocolHelpers;

static class RequiredResourceAttributes
{
    public static IReadOnlyDictionary<string, object> AddDefaults(
        IReadOnlyDictionary<string, object> resourceAttributes)
    {
        Dictionary<string, object>? updated = null;

        if (!HasNonWhiteSpaceStringAttribute(resourceAttributes, SemanticConventions.AttributeServiceName))
        {
            updated ??= resourceAttributes.ToDictionary(ra => ra.Key, ra => ra.Value);

            var defaultServiceName = "unknown_service";
            var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
            if (!string.IsNullOrWhiteSpace(moduleName))
                defaultServiceName += $":{moduleName}";

            updated[SemanticConventions.AttributeServiceName] = defaultServiceName;
        }

        if (!HasNonWhiteSpaceStringAttribute(resourceAttributes, SemanticConventions.AttributeTelemetrySdkName))
        {
            updated ??= resourceAttributes.ToDictionary(ra => ra.Key, ra => ra.Value);
            updated[SemanticConventions.AttributeTelemetrySdkName] = PackageIdentity.TelemetrySdkName;
        }

        if (!HasNonWhiteSpaceStringAttribute(resourceAttributes, SemanticConventions.AttributeTelemetrySdkLanguage))
        {
            updated ??= resourceAttributes.ToDictionary(ra => ra.Key, ra => ra.Value);
            updated[SemanticConventions.AttributeTelemetrySdkLanguage] = PackageIdentity.TelemetrySdkLanguage;
        }

        if (!HasNonWhiteSpaceStringAttribute(resourceAttributes, SemanticConventions.AttributeTelemetrySdkVersion))
        {
            updated ??= resourceAttributes.ToDictionary(ra => ra.Key, ra => ra.Value);
            updated[SemanticConventions.AttributeTelemetrySdkVersion] = PackageIdentity.GetTelemetrySdkVersion();
        }

        return updated ?? resourceAttributes;
    }

    static bool HasNonWhiteSpaceStringAttribute(IReadOnlyDictionary<string, object> attributes, string attributeName)
    {
        return attributes.TryGetValue(attributeName, out var attributeValue) &&
               attributeValue is string { } attributeString &&
               !string.IsNullOrWhiteSpace(attributeString);
    }
}
