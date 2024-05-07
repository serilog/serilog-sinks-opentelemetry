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

using Serilog.Core.Sinks.Batching;

namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Options type for controlling batching behavior.
/// </summary>
public class BatchedOpenTelemetrySinkOptions : OpenTelemetrySinkOptions
{
    const int DefaultBatchSizeLimit = 1000, DefaultBufferingTimeLimitSeconds = 2, DefaultQueueLimit = 100000;

    /// <summary>
    /// Options that control the sending of asynchronous log batches. When <c>null</c> a batch size of 1 is used.
    /// </summary>
    public BatchingOptions BatchingOptions { get; } = new()
    {
        EagerlyEmitFirstEvent = true,
        BatchSizeLimit = DefaultBatchSizeLimit,
        BufferingTimeLimit = TimeSpan.FromSeconds(DefaultBufferingTimeLimitSeconds),
        QueueLimit = DefaultQueueLimit
    };
}
