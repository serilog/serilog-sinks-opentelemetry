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

using Serilog;
using System;
using System.Threading;
using System.Diagnostics;

namespace SerilogSinksOpenTelemetryExample;

class Program
{
    static void Main(string[] args)
    {
        Random rand = new Random();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        ActivitySource source = new ActivitySource("test.example", "1.0.0");

        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithTraceIdAndSpanId()
            .Enrich.WithOpenTelemetryException()
            .WriteTo.OpenTelemetry(
                endpoint: "http://127.0.0.1:4317/v1/logs",
                resourceAttributes: new Dictionary<String, Object>() {
                        {"service.name", "test-logging-service"},
                        {"index", 10},
                        {"flag", true},
                        {"value", 3.14}
                },
                batchSizeLimit: 2,
                batchPeriod: 5,
                batchQueueLimit: 1000)
            .CreateLogger();

        for (int i = 0; i < 100; i++)
        {
            using (var activity = source.StartActivity("loop"))
            {
                var position = new { Latitude = rand.Next(-90, 91), Longitude = rand.Next(0, 361) };
                var elapsedMs = rand.Next(0, 101);

                log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);

                try {
                    throw new Exception("iteration #" + i);
                } catch (Exception ex) {
                    log.Error(ex, "count = {@Count}", i);
                }

                Console.WriteLine(i);
                Thread.Sleep(1000);
            }
        }
    }
}
