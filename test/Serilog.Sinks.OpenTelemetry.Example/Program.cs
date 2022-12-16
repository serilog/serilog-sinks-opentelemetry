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

namespace SerilogSinksOpenTelemetryExample;

class Program
{
    static void Main(string[] args)
    {
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.OpenTelemetry(endpoint: "http://127.0.0.1:4317",
            resourceAttributes: new Dictionary<String, Object>() {
                        {"service.name", "test-logging-service"},
                        {"index", 10},
                        {"flag", true},
                        {"value", 3.14}
                })
            .CreateLogger();

        var position = new { Latitude = 25, Longitude = 134 };
        var elapsedMs = 34;

        log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);

    }
}
