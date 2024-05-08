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
using Serilog.Core;
using Serilog.Sinks.OpenTelemetry;

// Without activity listeners present, trace and span ids are not collected.
using var listener = new ActivityListener();
listener.ShouldListenTo = _ => true;
listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
ActivitySource.AddActivityListener(listener);

var source = new ActivitySource("test.example", "1.0.0");

using (var grpcLogger = CreateLogger(OtlpProtocol.Grpc))
{
    using var activity = source.StartActivity("grpc-loop");
    SendLogs(grpcLogger, "grpc");
}

using (var httpLogger = CreateLogger(OtlpProtocol.HttpProtobuf))
{
    using var activity = source.StartActivity("http-loop");
    SendLogs(httpLogger, "http/protobuf");
}

static void SendLogs(ILogger logger, string protocol)
{
    var position = new { Latitude = Random.Shared.Next(-90, 91), Longitude = Random.Shared.Next(0, 361) };
    var elapsedMs = Random.Shared.Next(0, 101);
    var roll = Random.Shared.Next(0, 7);

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

static Logger CreateLogger(OtlpProtocol protocol)
{
    var endpoint = protocol == OtlpProtocol.HttpProtobuf ?
        "http://localhost:4318/v1/logs" :
        "http://localhost:4317";

    return new LoggerConfiguration()
        .WriteTo.OpenTelemetry(options => {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            // Prevent tracing of outbound requests from the sink
            options.HttpMessageHandler = new SocketsHttpHandler { ActivityHeadersPropagator = null };
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
            options.BatchingOptions.BatchSizeLimit = 700;
            options.BatchingOptions.BufferingTimeLimit = TimeSpan.FromSeconds(1);
            options.BatchingOptions.QueueLimit = 10;
        })
        .CreateLogger();
}
