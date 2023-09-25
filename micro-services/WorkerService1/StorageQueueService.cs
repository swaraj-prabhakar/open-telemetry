using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using OpenTelemetry;
using System.Diagnostics;
using System.Text;
using Utils;

namespace WorkerService1;

public class StorageQueueService
{
    private readonly IConfiguration _config;

    public StorageQueueService(IConfiguration config)
    {
        _config = config;
    }

    public async Task Dequeue(string queueName, CancellationToken ct)
    {
        string connectionString = _config["StorageConnectionString"];

        QueueClient queueClient = new (connectionString, queueName);

        if (await queueClient.ExistsAsync(ct))
        {
            QueueMessage retrievedMessage = await queueClient.ReceiveMessageAsync(null, ct);

            var msg = JsonConvert.DeserializeObject<Message>(retrievedMessage.Body.ToString());
            var parentContext = DiagnosticsConfig.Propagator.Extract(default, msg, ExtractTraceContextFromAttributes);
            Baggage.Current = parentContext.Baggage;

            using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"Dequeue : {queueName}", ActivityKind.Consumer, parentContext.ActivityContext);
            activity?.SetTag("queue.name", queueName);
            activity?.SetTag("queue.dequeue.time", DateTime.UtcNow.ToString());

            Console.WriteLine($"Dequeued message: '{retrievedMessage.Body}'");

            await queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt, ct);
        }
    }

    public async Task<bool> MessageExistsAsync(string queueName)
    {
        string connectionString = _config["StorageConnectionString"];

        QueueClient queueClient = new (connectionString, queueName);

        if (await queueClient.ExistsAsync())
        {
            QueueProperties properties = queueClient.GetProperties();

            return properties.ApproximateMessagesCount > 0;
        }

        return false;
    }

    private IEnumerable<string> ExtractTraceContextFromAttributes(Message? message, string key)
    {
        if (message != null && message.Attributes.TryGetValue(key, out var value))
        {
            var val = value as string;
            return new[] { val };
        }

        return Enumerable.Empty<string>();
    }
}
