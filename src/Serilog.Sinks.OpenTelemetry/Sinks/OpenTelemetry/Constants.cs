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

using System.Diagnostics;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry;

static class Constants
{
    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's start timestamp. Log events will be interpreted as spans if and only if
    /// this property contains a scalar <see cref="DateTime"/> value, as well as
    /// valid <see cref="LogEvent.TraceId"/> and <see cref="LogEvent.SpanId"/> values.
    /// </summary>
    public const string SpanStartTimestampPropertyName = "SpanStartTimestamp";
    
    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's <see cref="System.Diagnostics.Activity.ParentId"/>, if there is one.
    /// </summary>
    public const string ParentSpanIdPropertyName = "ParentSpanId";

    /// <summary>
    /// The name of the entry in <see cref="LogEvent.Properties"/> that carries a
    /// span's kind. The value will be an <see cref="ActivityKind"/>. The span kind is
    /// unset for <see cref="ActivityKind.Internal"/> spans: any span without an explicit
    /// kind should be assumed to be internal.
    /// </summary>
    public const string SpanKindPropertyName = "SpanKind";
}
