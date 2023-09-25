using Azure.Storage.Queues;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using Utils;

namespace WebApiProject1;

public class StorageQueueService
{
    private readonly IConfiguration _config;

    public StorageQueueService(IConfiguration config)
    {
        _config = config;
    }

    public async Task Enqueue(string queueName, string message, CancellationToken ct)
    {
        string connectionString = _config["StorageConnectionString"];
        QueueClient queueClient = new (connectionString, queueName);

        await queueClient.CreateIfNotExistsAsync(null, ct);

        if (await queueClient.ExistsAsync(ct))
        {
            using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"Enqueue : {queueName}", ActivityKind.Producer);
            activity?.SetTag("queue.name", queueName);
            activity?.SetTag("queue.enqueue.time", DateTime.UtcNow.ToString());

            var msg = new Message
            {
                Data = message,
            };

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            DiagnosticsConfig.Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), msg, InjectTraceContextIntoAttributes);

            await queueClient.SendMessageAsync(JsonConvert.SerializeObject(msg), ct);
        }
    }

    private void InjectTraceContextIntoAttributes(Message message, string key, string value)
    {
        if (message.Attributes == null)
        {
            message.Attributes = new Dictionary<string, object>();
        }

        message.Attributes[key] = value;
    }
}
