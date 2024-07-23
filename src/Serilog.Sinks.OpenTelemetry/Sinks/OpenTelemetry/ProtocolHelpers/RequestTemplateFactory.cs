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

using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Serilog.Sinks.OpenTelemetry.ProtocolHelpers;

static class RequestTemplateFactory
{
    const string OpenTelemetrySchemaUrl = "https://opentelemetry.io/schemas/v1.13.0";

    public static ScopeLogs CreateScopeLogs(string? scopeName)
    {
        var scope = scopeName != null ? new InstrumentationScope
        {
            Name = scopeName,
        } : null;

        return new ScopeLogs
        {
            Scope = scope
        };
    }

    public static ScopeSpans CreateScopeSpans(string? scopeName)
    {
        var scope = scopeName != null ? new InstrumentationScope
        {
            Name = scopeName,
        } : null;

        return new ScopeSpans()
        {
            Scope = scope
        };
    }

    public static ResourceLogs CreateResourceLogs(IReadOnlyDictionary<string, object> resourceAttributes)
    {
        var resourceLogs = new ResourceLogs();

        var attrs = ToResourceAttributes(resourceAttributes);

        var resource = new Resource();
        resource.Attributes.AddRange(attrs);
        resourceLogs.Resource = resource;
        resourceLogs.SchemaUrl = OpenTelemetrySchemaUrl;

        return resourceLogs;
    }

    public static ResourceSpans CreateResourceSpans(IReadOnlyDictionary<string, object> resourceAttributes)
    {
        var resourceSpans = new ResourceSpans();

        var attrs = ToResourceAttributes(resourceAttributes);

        var resource = new Resource();
        resource.Attributes.AddRange(attrs);
        resourceSpans.Resource = resource;
        resourceSpans.SchemaUrl = OpenTelemetrySchemaUrl;

        return resourceSpans;
    }

    static RepeatedField<KeyValue> ToResourceAttributes(IReadOnlyDictionary<string, object> resourceAttributes)
    {
        var attributes = new RepeatedField<KeyValue>();
        foreach (var entry in resourceAttributes)
        {
            var v = PrimitiveConversions.ToOpenTelemetryPrimitive(entry.Value);
            var kv = new KeyValue
            {
                Value = v,
                Key = entry.Key
            };
            attributes.Add(kv);
        }
        return attributes;
    }
}
