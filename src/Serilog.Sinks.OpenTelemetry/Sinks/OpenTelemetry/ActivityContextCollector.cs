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
using System.Runtime.CompilerServices;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Uses <see cref="ConditionalWeakTable{TKey,TValue}"/> to maintain a mapping from Serilog <see cref="LogEvent"/> to
/// a captured trace and span id from <see cref="Activity.Current"/>. This enables the activity context to be captured
/// at the point logging occurs, while construction of the resulting <see cref="LogRecord"/> is deferred to background
/// processing.
/// </summary>
/// <remarks>CWT is necessary because queue limits, batch dropping, and other constraints make it hard to guarantee that
/// no space leak will occur due to log events being added that are never subsequently removed by the background
/// worker.</remarks>
sealed class ActivityContextCollector
{
#if FEATURE_ACTIVITY
    record CollectedContext(ActivityTraceId TraceId, ActivitySpanId SpanId);
    
    readonly ConditionalWeakTable<LogEvent, CollectedContext> _context = new();
    
    public void CollectFor(LogEvent logEvent)
    {
        if (Activity.Current != null)
            _context.AddOrUpdate(logEvent, new CollectedContext(Activity.Current.TraceId, Activity.Current.SpanId));
    }

    public (ActivityTraceId, ActivitySpanId)? GetFor(LogEvent logEvent)
    {
        if (_context.TryGetValue(logEvent, out var context))
            return (context.TraceId, context.SpanId);
        
        return null;
    }
#endif
}
