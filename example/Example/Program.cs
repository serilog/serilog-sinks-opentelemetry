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

// ReSharper disable ExplicitCallerInfoArgument

using Serilog;
using System.Diagnostics;
using Serilog.Sinks.OpenTelemetry;

namespace Example;

static class Program
{
    static readonly Random Rand = new();

    static void Main()
    {
        // create an ActivitySource (that is listened to) for creating an Activity
        // to test the trace and span ID enricher
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;

        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("test.example", "1.0.0");

        // Create the loggers to send to gRPC and to HTTP.
        var grpcLogger = GetLogger(OtlpProtocol.Grpc);
        var httpLogger = GetLogger(OtlpProtocol.HttpProtobuf);

        using (source.StartActivity("grpc-loop"))
        {
            SendLogs(grpcLogger, "grpc");
        }

        using (source.StartActivity("http-loop"))
        {
            SendLogs(httpLogger, "http/protobuf");
        }

        Thread.Sleep(5000);
    }

    static void SendLogs(ILogger logger, string protocol)
    {
        var position = new { Latitude = Rand.Next(-90, 91), Longitude = Rand.Next(0, 361) };
        var elapsedMs = Rand.Next(0, 101);
        var roll = Rand.Next(0, 7);

        logger
            .ForContext("Elapsed", elapsedMs)
            .ForContext("Protocol", protocol)
            .Information("The position is {@Position}", position);

        try
        {
            throw new Exception(protocol);
        }
        catch (Exception ex)
        {
            logger.ForContext("protocol", protocol).Error(ex, "Error on roll {Roll}", roll);
        }
    }

    static ILogger GetLogger(OtlpProtocol protocol)
    {
        var endpoint = protocol == OtlpProtocol.HttpProtobuf ?
            "http://localhost:4318/v1/logs" :
            "http://localhost:4317";

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.OpenTelemetry(options => {
                options.Endpoint = endpoint;
                options.Protocol = protocol;
                options.IncludedData = 
                    IncludedData.SpanIdField
                    | IncludedData.TraceIdField
                    | IncludedData.MessageTemplateTextAttribute
                    | IncludedData.MessageTemplateMD5HashAttribute;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "test-logging-service",
                    ["index"] = 10,
                    ["flag"] = true,
                    ["pi"] = 3.14
                };
                options.Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Basic dXNlcjphYmMxMjM=", // user:abc123
                };
                options.BatchingOptions.BatchSizeLimit = 2;
                options.BatchingOptions.Period = TimeSpan.FromSeconds(2);
                options.BatchingOptions.QueueLimit = 10;
            })
            .CreateLogger();
    }
}
