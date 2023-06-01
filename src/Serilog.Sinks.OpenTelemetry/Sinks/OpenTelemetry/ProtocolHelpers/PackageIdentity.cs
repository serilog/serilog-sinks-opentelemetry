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

using System.Reflection;

namespace Serilog.Sinks.OpenTelemetry.ProtocolHelpers;

static class PackageIdentity
{
    public static string GetInstrumentationScopeName()
    {
        return typeof(RequestTemplateFactory).Assembly.GetName().Name
               // Best we know about this, if it occurs.
               ?? throw new InvalidOperationException("Sink assembly name could not be retrieved.");
    }

    public static string GetInstrumentationScopeVersion()
    {
        return typeof(RequestTemplateFactory).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    }

    public const string TelemetrySdkName = "serilog";
    
    public static string GetTelemetrySdkVersion() => GetInstrumentationScopeVersion();

    public const string TelemetrySdkLanguage = "dotnet";
}
