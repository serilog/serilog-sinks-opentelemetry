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

using System;
using System.Threading;
using System.Diagnostics;
using Serilog;


namespace SerilogSinksOpenTelemetryExample;

class Program
{
    static void Main(string[] args)
    {
        Random rand = new Random();

        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithTraceIdAndSpanId()
            .WriteTo.OpenTelemetry(
                endpoint: "http://127.0.0.1:4317",
                resourceAttributes: new Dictionary<String, Object>() {
                        {"service.name", "test-logging-service"},
                        {"index", 10},
                        {"flag", true},
                        {"value", 3.14}
                },
                batchSizeLimit: 10,
                batchPeriod: 5,
                batchQueueLimit: 1000)
            .CreateLogger();

        for (int i = 0; i < 100; i++)
        {
            var activity = new Activity("loop");
            activity.Start();

            var position = new { Latitude = rand.Next(-90, 91), Longitude = rand.Next(0, 361) };
            var elapsedMs = rand.Next(0, 101);

            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error("count = {@Count}", i);

            activity.Stop();
            Console.WriteLine(i);
            Thread.Sleep(1000);
        }
    }
}
