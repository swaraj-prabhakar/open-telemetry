using Azure.Messaging.ServiceBus;
using OpenTelemetry;
using System.Diagnostics;

namespace WorkerService1;

public class ServiceBusQueueService
{
    private readonly IConfiguration _config;

    public ServiceBusQueueService(IConfiguration config)
    {
        _config = config;
    }

    public async Task Dequeue(string queueName, CancellationToken ct)
    {
        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        var client = new ServiceBusClient(_config["ServiceBusConnectionString"], clientOptions);

        var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());


        processor.ProcessMessageAsync += MessageHandler;

        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync();
    }

    private IEnumerable<string> ExtractTraceContextFromApplicationProperties(ServiceBusReceivedMessage? message, string key)
    {
        if (message != null && message.ApplicationProperties.TryGetValue(key, out var value))
        {
            var val = value as string;
            return new[] { val };
        }

        return Enumerable.Empty<string>();
    }

    async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        Console.WriteLine($"Received: {body}");

        var parentContext = DiagnosticsConfig.Propagator.Extract(default, args.Message, ExtractTraceContextFromApplicationProperties);
        Baggage.Current = parentContext.Baggage;

        using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"Dequeue : {args.FullyQualifiedNamespace}", ActivityKind.Consumer, parentContext.ActivityContext);
        activity?.SetTag("queue.name", args.FullyQualifiedNamespace);
        activity?.SetTag("queue.dequeue.time", DateTime.UtcNow.ToString());

        await args.CompleteMessageAsync(args.Message);
    }

    Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}
