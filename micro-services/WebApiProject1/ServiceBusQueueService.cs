using Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace WebApiProject1;

public class ServiceBusQueueService
{
    private readonly IConfiguration _config;

    public ServiceBusQueueService(IConfiguration config)
    {
        _config = config;
    }

    public async Task Enqueue(string queueName, string message, CancellationToken ct)
    {
        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        var client = new ServiceBusClient(_config["ServiceBusConnectionString"], clientOptions);
        var sender = client.CreateSender(queueName);

        try
        {
            using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"Enqueue : {queueName}", ActivityKind.Producer);
            activity?.SetTag("queue.name", queueName);
            activity?.SetTag("queue.enqueue.time", DateTime.UtcNow.ToString());

            var msg = new ServiceBusMessage(message);

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            DiagnosticsConfig.Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), msg, InjectTraceContextIntoApplicationProperties);

            await sender.SendMessageAsync(msg);
        }
        finally
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }

    private void InjectTraceContextIntoApplicationProperties(ServiceBusMessage message, string key, string value)
    {
        message.ApplicationProperties[key] = value;
    }
}
