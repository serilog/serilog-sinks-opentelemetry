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

namespace Serilog.Sinks.OpenTelemetry;

internal static class OpenTelemetryUtils
{
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

        var scopeName = OpenTelemetryUtils.GetScopeName();
        if (scopeName != null)
        {
            scope.Name = scopeName;
        }

        var scopeVersion = OpenTelemetryUtils.GetScopeVersion();
        if (scopeVersion != null)
        {
            scope.Version = scopeVersion;
        }

        return scope;
    }

    static ResourceLogs CreateResourceLogs(IDictionary<string, Object>? resourceAttributes)
    {
        var resourceLogs = new ResourceLogs();

        var attrs = Convert.ToResourceAttributes(resourceAttributes);
        if (attrs != null)
        {
            var resource = new Resource();
            resource.Attributes.AddRange(attrs);
            resourceLogs.Resource = resource;
            resourceLogs.SchemaUrl = Convert.SCHEMA_URL;
        }

        return resourceLogs;
    }

    static ScopeLogs CreateEmptyScopeLogs()
    {
        var scopeLogs = new ScopeLogs();
        scopeLogs.Scope = CreateInstrumentationScope();
        scopeLogs.SchemaUrl = Convert.SCHEMA_URL;

        return scopeLogs;
    }

    internal static ExportLogsServiceRequest CreateRequestTemplate(IDictionary<string, Object>? resourceAttributes)
    {
        var scopeTemplate = CreateInstrumentationScope();

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
        catch (Exception) { }
    }
}
