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

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using System.Reflection;
using Google.Protobuf.Collections;
using Serilog.Debugging;

namespace Serilog.Sinks.OpenTelemetry;

static class OpenTelemetryUtils
{
    const string OpenTelemetrySchemaUrl = "https://opentelemetry.io/schemas/v1.13.0";
    
    static string? GetScopeName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name;
    }

    static string? GetScopeVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }

    static InstrumentationScope CreateInstrumentationScope()
    {
        var scope = new InstrumentationScope();

        var scopeName = GetScopeName();
        if (scopeName != null)
        {
            scope.Name = scopeName;
        }

        var scopeVersion = GetScopeVersion();
        if (scopeVersion != null)
        {
            scope.Version = scopeVersion;
        }

        return scope;
    }

    static ResourceLogs CreateResourceLogs(IDictionary<string, object>? resourceAttributes)
    {
        var resourceLogs = new ResourceLogs();

        var attrs = ToResourceAttributes(resourceAttributes);

        var resource = new Resource();
        resource.Attributes.AddRange(attrs);
        resourceLogs.Resource = resource;
        resourceLogs.SchemaUrl = OpenTelemetrySchemaUrl;

        return resourceLogs;
    }

    static ScopeLogs CreateEmptyScopeLogs()
    {
        var scopeLogs = new ScopeLogs
        {
            Scope = CreateInstrumentationScope(),
            SchemaUrl = OpenTelemetrySchemaUrl
        };

        return scopeLogs;
    }

    internal static ExportLogsServiceRequest CreateRequestTemplate(IDictionary<string, object>? resourceAttributes)
    {
        var resourceLogs = CreateResourceLogs(resourceAttributes);
        resourceLogs.ScopeLogs.Add(CreateEmptyScopeLogs());

        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);

        return request;
    }

    internal static void Add(ExportLogsServiceRequest request, LogRecord logRecord)
    {
        try
        {
            request.ResourceLogs.ElementAt(0).ScopeLogs.ElementAt(0).LogRecords.Add(logRecord);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Log record could not be added to ExportLogsServiceRequest: {0}", ex);
        }
    }
    
    static RepeatedField<KeyValue> ToResourceAttributes(IDictionary<string, object>? resourceAttributes)
    {
        var attributes = new RepeatedField<KeyValue>();
        if (resourceAttributes != null)
        {
            foreach (var entry in resourceAttributes)
            {
                var v = ConvertUtils.ToOpenTelemetryPrimitive(entry.Value);
                if (v != null)
                {
                    var kv = new KeyValue
                    {
                        Value = v,
                        Key = entry.Key
                    };
                    attributes.Add(kv);
                }
            }
        }
        return attributes;
    }
}
