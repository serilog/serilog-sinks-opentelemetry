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

using static Serilog.Sinks.OpenTelemetry.OpenTelemetrySink;
using Serilog;
using System;
using System.Threading;
using System.Diagnostics;

namespace SerilogSinksOpenTelemetryExample;

class Program
{
    static readonly Random _rand = new Random();

    static void Main(string[] args)
    {
        // create an ActivitySource (that is listened to) for creating an Activity
        // to test the trace and span ID enricher
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        ActivitySource source = new ActivitySource("test.example", "1.0.0");

        // Create the loggers to send to gRPC and to HTTP.
        var grpcLogger = GetLogger(OtlpProtocol.GrpcProtobuf);
        var httpLogger = GetLogger(OtlpProtocol.HttpProtobuf);

        using (var activity = source.StartActivity("grpc-loop"))
        {
            SendLogs(grpcLogger, "grpc/protobuf");
        }

        using (var activity = source.StartActivity("http-loop"))
        {
            SendLogs(httpLogger, "http/protobuf");
        }

        Thread.Sleep(5000);
    }

    static void SendLogs(ILogger logger, string protocol)
    {
        var position = new { Latitude = _rand.Next(-90, 91), Longitude = _rand.Next(0, 361) };
        var elapsedMs = _rand.Next(0, 101);
        var roll = _rand.Next(0, 7);

        logger
        .ForContext("Elapsed", elapsedMs)
        .ForContext("protocol", protocol)
        .Information("{@Position}", position);

        try
        {
            throw new Exception(protocol);
        }
        catch (Exception ex)
        {
            logger.ForContext("protocol", protocol).Error(ex, "{@Roll}", roll);
        }
    }

    static ILogger GetLogger(OtlpProtocol protocol)
    {
        var port = (protocol == OtlpProtocol.HttpProtobuf) ? 4318 : 4317;
        var endpoint = String.Format("http://127.0.0.1:{0}/v1/logs", port);

        return new LoggerConfiguration()
          .MinimumLevel.Information()
          .Enrich.WithTraceIdAndSpanId()
          .WriteTo.OpenTelemetry(
              endpoint: endpoint,
              protocol: protocol,
              resourceAttributes: new Dictionary<string, Object>() {
                        {"service.name", "test-logging-service"},
                        {"index", 10},
                        {"flag", true},
                        {"pi", 3.14}
              },
              headers: new Dictionary<string, string>() {
                    {"Authorization", "Basic dXNlcjphYmMxMjM="}, // user:abc123
              },
              batchSizeLimit: 2,
              batchPeriod: 2,
              batchQueueLimit: 10)
          .CreateLogger();
    }
}
